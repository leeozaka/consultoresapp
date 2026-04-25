package main

import (
	"crypto/hmac"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"net/http/httptest"
	"strings"
	"sync"
	"testing"
	"time"
)

type receivedWebhook struct {
	Type      string
	Signature string
	Payload   []byte
}

func TestHandleCreateCheckoutSession_EmitsCompletedAndPaidWebhooks(t *testing.T) {
	withDeterministicSimulator(t, 1, func(received <-chan receivedWebhook) {
		body := `{"price_id":"price_test","success_url":"http://ok","cancel_url":"http://cancel","metadata":{"tenant_id":"tenant-123","plan_id":"plan-456"}}`
		req := httptest.NewRequest(http.MethodPost, "/v1/checkout/sessions", strings.NewReader(body))
		w := httptest.NewRecorder()

		handleCreateCheckoutSession(w, req)

		if w.Code != http.StatusOK {
			t.Fatalf("expected 200, got %d", w.Code)
		}

		first := waitForWebhook(t, received)
		if first.Type != "checkout.session.completed" {
			t.Fatalf("expected first webhook to be checkout.session.completed, got %s", first.Type)
		}
		assertValidSignature(t, first.Signature, first.Payload)

		second := waitForWebhook(t, received)
		if second.Type != "invoice.paid" {
			t.Fatalf("expected second webhook to be invoice.paid, got %s", second.Type)
		}
		assertValidSignature(t, second.Signature, second.Payload)
	})
}

func TestHandleCreateCheckoutSession_EmitsExpiredWebhookWithoutInvoice(t *testing.T) {
	withDeterministicSimulator(t, 0, func(received <-chan receivedWebhook) {
		body := `{"price_id":"price_test","success_url":"http://ok","cancel_url":"http://cancel","metadata":{"tenant_id":"tenant-123"}}`
		req := httptest.NewRequest(http.MethodPost, "/v1/checkout/sessions", strings.NewReader(body))
		w := httptest.NewRecorder()

		handleCreateCheckoutSession(w, req)

		if w.Code != http.StatusOK {
			t.Fatalf("expected 200, got %d", w.Code)
		}

		first := waitForWebhook(t, received)
		if first.Type != "checkout.session.expired" {
			t.Fatalf("expected expired webhook, got %s", first.Type)
		}
		assertValidSignature(t, first.Signature, first.Payload)

		select {
		case evt := <-received:
			t.Fatalf("expected no invoice webhook, got %s", evt.Type)
		case <-time.After(100 * time.Millisecond):
		}
	})
}

func withDeterministicSimulator(t *testing.T, success float64, run func(received <-chan receivedWebhook)) {
	t.Helper()

	originalWebhookSecret := webhookSecret
	originalCallbackURL := callbackURL
	originalSuccessRate := successRate
	originalSleepFn := sleepFn
	originalRandFloat64Fn := randFloat64Fn
	originalRandIntnFn := randIntnFn

	webhookSecret = "whsec_integration_test"
	successRate = success
	sleepFn = func(time.Duration) {}
	randFloat64Fn = func() float64 { return 0 }
	randIntnFn = func(int) int { return 0 }

	received := make(chan receivedWebhook, 4)
	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		payload, err := io.ReadAll(r.Body)
		if err != nil {
			t.Fatalf("failed to read webhook body: %v", err)
		}

		var event WebhookEvent
		if err := json.Unmarshal(payload, &event); err != nil {
			t.Fatalf("failed to decode webhook event: %v", err)
		}

		received <- receivedWebhook{
			Type:      event.Type,
			Signature: r.Header.Get("Stripe-Signature"),
			Payload:   payload,
		}
		w.WriteHeader(http.StatusOK)
	}))
	defer server.Close()

	callbackURL = server.URL
	defer func() {
		webhookSecret = originalWebhookSecret
		callbackURL = originalCallbackURL
		successRate = originalSuccessRate
		sleepFn = originalSleepFn
		randFloat64Fn = originalRandFloat64Fn
		randIntnFn = originalRandIntnFn
	}()

	run(received)
}

func waitForWebhook(t *testing.T, received <-chan receivedWebhook) receivedWebhook {
	t.Helper()

	select {
	case evt := <-received:
		return evt
	case <-time.After(2 * time.Second):
		t.Fatal("timed out waiting for webhook")
		return receivedWebhook{}
	}
}

func assertValidSignature(t *testing.T, signatureHeader string, payload []byte) {
	t.Helper()

	parts := strings.Split(signatureHeader, ",")
	if len(parts) != 2 {
		t.Fatalf("expected timestamp and signature, got %q", signatureHeader)
	}

	var timestamp, signature string
	for _, part := range parts {
		kv := strings.SplitN(part, "=", 2)
		if len(kv) != 2 {
			t.Fatalf("invalid signature component %q", part)
		}
		switch kv[0] {
		case "t":
			timestamp = kv[1]
		case "v1":
			signature = kv[1]
		}
	}

	if timestamp == "" || signature == "" {
		t.Fatalf("missing timestamp or signature in %q", signatureHeader)
	}

	signedPayload := fmt.Sprintf("%s.%s", timestamp, string(payload))
	mac := hmac.New(sha256.New, []byte(webhookSecret))
	mac.Write([]byte(signedPayload))
	expected := hex.EncodeToString(mac.Sum(nil))

	if signature != expected {
		t.Fatalf("signature mismatch: got %s want %s", signature, expected)
	}
}

func TestDeliverWebhook_PostsExpectedHeadersAndPayload(t *testing.T) {
	originalWebhookSecret := webhookSecret
	originalCallbackURL := callbackURL
	defer func() {
		webhookSecret = originalWebhookSecret
		callbackURL = originalCallbackURL
	}()

	webhookSecret = "whsec_delivery_test"

	var (
		mu        sync.Mutex
		received  WebhookEvent
		signature string
	)

	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		payload, err := io.ReadAll(r.Body)
		if err != nil {
			t.Fatalf("failed to read body: %v", err)
		}
		if err := json.Unmarshal(payload, &received); err != nil {
			t.Fatalf("failed to unmarshal webhook: %v", err)
		}
		mu.Lock()
		signature = r.Header.Get("Stripe-Signature")
		mu.Unlock()
		w.WriteHeader(http.StatusOK)
	}))
	defer server.Close()

	callbackURL = server.URL

	event := WebhookEvent{
		ID:      "evt_test_direct",
		Object:  "event",
		Type:    "invoice.paid",
		Created: time.Now().Unix(),
		Data:    WebhookEventData{Object: json.RawMessage(`{"id":"in_test_123"}`)},
	}

	deliverWebhook(event, "in_test_123")

	mu.Lock()
	defer mu.Unlock()

	if received.ID != event.ID {
		t.Fatalf("expected event id %s, got %s", event.ID, received.ID)
	}
	assertValidSignature(t, signature, mustJSON(t, received))
}

func mustJSON(t *testing.T, v any) []byte {
	t.Helper()
	payload, err := json.Marshal(v)
	if err != nil {
		t.Fatalf("failed to marshal value: %v", err)
	}
	return payload
}
