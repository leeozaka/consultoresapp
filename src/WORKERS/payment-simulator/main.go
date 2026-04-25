package main

import (
	"bytes"
	"crypto/hmac"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"log"
	"math/rand"
	"net/http"
	"os"
	"strconv"
	"strings"
	"time"
)

type CheckoutSessionRequest struct {
	PriceID    string            `json:"price_id"`
	Amount     int64             `json:"amount"`
	Currency   string            `json:"currency"`
	SuccessURL string            `json:"success_url"`
	CancelURL  string            `json:"cancel_url"`
	Mode       string            `json:"mode"`
	Metadata   map[string]string `json:"metadata"`
}

type CheckoutSession struct {
	ID            string            `json:"id"`
	Object        string            `json:"object"`
	URL           string            `json:"url"`
	Status        string            `json:"status"`
	Mode          string            `json:"mode"`
	PaymentIntent string            `json:"payment_intent"`
	Subscription  string            `json:"subscription"`
	AmountTotal   int64             `json:"amount_total"`
	Currency      string            `json:"currency"`
	Metadata      map[string]string `json:"metadata"`
	SuccessURL    string            `json:"success_url"`
	CancelURL     string            `json:"cancel_url"`
	CreatedAt     int64             `json:"created"`
}

type InvoiceObject struct {
	ID            string            `json:"id"`
	Object        string            `json:"object"`
	Subscription  string            `json:"subscription"`
	PaymentIntent string            `json:"payment_intent"`
	AmountPaid    int64             `json:"amount_paid"`
	AmountTotal   int64             `json:"amount_total"`
	Currency      string            `json:"currency"`
	Status        string            `json:"status"`
	Metadata      map[string]string `json:"metadata"`
	Created       int64             `json:"created"`
}

type WebhookEvent struct {
	ID      string           `json:"id"`
	Object  string           `json:"object"`
	Type    string           `json:"type"`
	Created int64            `json:"created"`
	Data    WebhookEventData `json:"data"`
}

type WebhookEventData struct {
	Object json.RawMessage `json:"object"`
}

var (
	webhookSecret string
	callbackURL   string
	successRate   float64
	sleepFn       = time.Sleep
	randIntnFn    = rand.Intn
	randFloat64Fn = rand.Float64
)

func main() {
	webhookSecret = getEnv("WEBHOOK_SECRET", "whsec_test_secret")
	callbackURL = getEnv("CALLBACK_URL", "http://localhost:5001/api/webhooks/stripe")
	successRate = getEnvFloat("SUCCESS_RATE", 0.9)
	port := getEnv("PORT", "8080")

	mux := http.NewServeMux()
	mux.HandleFunc("POST /v1/checkout/sessions", handleCreateCheckoutSession)
	mux.HandleFunc("GET /health", handleHealth)

	log.Printf("payment-simulator listening on :%s (callback=%s, success_rate=%.0f%%)", port, callbackURL, successRate*100)
	if err := http.ListenAndServe(":"+port, mux); err != nil {
		log.Fatal(err)
	}
}

func handleHealth(w http.ResponseWriter, _ *http.Request) {
	w.WriteHeader(http.StatusOK)
	fmt.Fprint(w, "ok")
}

func handleCreateCheckoutSession(w http.ResponseWriter, r *http.Request) {
	var req CheckoutSessionRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, `{"error": "invalid json"}`, http.StatusBadRequest)
		return
	}

	sessionID := fmt.Sprintf("cs_test_%s", randomID(24))
	paymentIntentID := fmt.Sprintf("pi_test_%s", randomID(24))
	subscriptionID := fmt.Sprintf("sub_test_%s", randomID(24))
	now := time.Now().Unix()

	session := CheckoutSession{
		ID:            sessionID,
		Object:        "checkout.session",
		URL:           fmt.Sprintf("http://localhost:8080/checkout/%s", sessionID),
		Status:        "open",
		Mode:          coalesce(req.Mode, "payment"),
		PaymentIntent: paymentIntentID,
		Subscription:  subscriptionID,
		AmountTotal:   parsePriceAmount(req),
		Currency:      strings.ToLower(coalesce(req.Currency, "brl")),
		Metadata:      req.Metadata,
		SuccessURL:    req.SuccessURL,
		CancelURL:     req.CancelURL,
		CreatedAt:     now,
	}

	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(session)

	go scheduleWebhookCallback(session)
}

func scheduleWebhookCallback(session CheckoutSession) {
	delay := time.Duration(2+randIntnFn(4)) * time.Second
	sleepFn(delay)

	succeeded := randFloat64Fn() < successRate

	if succeeded {
		session.Status = "complete"
	} else {
		session.Status = "expired"
	}

	sessionJSON, _ := json.Marshal(session)

	eventType := "checkout.session.completed"
	if !succeeded {
		eventType = "checkout.session.expired"
	}

	event := WebhookEvent{
		ID:      fmt.Sprintf("evt_test_%s", randomID(24)),
		Object:  "event",
		Type:    eventType,
		Created: time.Now().Unix(),
		Data: WebhookEventData{
			Object: json.RawMessage(sessionJSON),
		},
	}

	deliverWebhook(event, session.ID)

	if succeeded {
		go scheduleInvoiceWebhook(session)
	}
}

func scheduleInvoiceWebhook(session CheckoutSession) {
	delay := time.Duration(3+randIntnFn(3)) * time.Second
	sleepFn(delay)

	invoicePaid := randFloat64Fn() < successRate

	invoice := InvoiceObject{
		ID:            fmt.Sprintf("in_test_%s", randomID(24)),
		Object:        "invoice",
		Subscription:  session.Subscription,
		PaymentIntent: session.PaymentIntent,
		AmountPaid:    session.AmountTotal,
		AmountTotal:   session.AmountTotal,
		Currency:      session.Currency,
		Metadata:      session.Metadata,
		Created:       time.Now().Unix(),
	}

	var eventType string
	if invoicePaid {
		invoice.Status = "paid"
		eventType = "invoice.paid"
	} else {
		invoice.Status = "open"
		eventType = "invoice.payment_failed"
	}

	invoiceJSON, _ := json.Marshal(invoice)

	event := WebhookEvent{
		ID:      fmt.Sprintf("evt_test_%s", randomID(24)),
		Object:  "event",
		Type:    eventType,
		Created: time.Now().Unix(),
		Data: WebhookEventData{
			Object: json.RawMessage(invoiceJSON),
		},
	}

	deliverWebhook(event, invoice.ID)
}

func deliverWebhook(event WebhookEvent, resourceID string) {
	payload, _ := json.Marshal(event)

	timestamp := strconv.FormatInt(time.Now().Unix(), 10)
	signature := computeStripeSignature(timestamp, payload)
	sigHeader := fmt.Sprintf("t=%s,v1=%s", timestamp, signature)

	req, err := http.NewRequest("POST", callbackURL, bytes.NewReader(payload))
	if err != nil {
		log.Printf("failed to create webhook request: %v", err)
		return
	}
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("Stripe-Signature", sigHeader)

	client := &http.Client{Timeout: 10 * time.Second}
	resp, err := client.Do(req)
	if err != nil {
		log.Printf("webhook delivery failed for %s: %v", resourceID, err)
		return
	}
	defer resp.Body.Close()

	log.Printf("webhook delivered resource=%s type=%s status=%d", resourceID, event.Type, resp.StatusCode)
}

func computeStripeSignature(timestamp string, payload []byte) string {
	signedPayload := fmt.Sprintf("%s.%s", timestamp, string(payload))
	mac := hmac.New(sha256.New, []byte(webhookSecret))
	mac.Write([]byte(signedPayload))
	return hex.EncodeToString(mac.Sum(nil))
}

func randomID(length int) string {
	const charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ012369"
	b := make([]byte, length)
	for i := range b {
		b[i] = charset[rand.Intn(len(charset))]
	}
	return string(b)
}

func parsePriceAmount(req CheckoutSessionRequest) int64 {
	if req.Amount > 0 {
		return req.Amount
	}

	return 9990
}

func coalesce(values ...string) string {
	for _, v := range values {
		if v != "" {
			return v
		}
	}
	return ""
}

func getEnv(key, fallback string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return fallback
}

func getEnvFloat(key string, fallback float64) float64 {
	v := os.Getenv(key)
	if v == "" {
		return fallback
	}
	f, err := strconv.ParseFloat(v, 64)
	if err != nil {
		return fallback
	}
	return f
}
