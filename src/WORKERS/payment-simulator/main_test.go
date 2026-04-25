package main

import (
	"crypto/hmac"
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"net/http"
	"net/http/httptest"
	"os"
	"strings"
	"testing"
)

func TestHandleHealth(t *testing.T) {
	req := httptest.NewRequest("GET", "/health", nil)
	w := httptest.NewRecorder()
	handleHealth(w, req)

	if w.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", w.Code)
	}
}

func TestHandleCreateCheckoutSession_ReturnsSession(t *testing.T) {
	body := `{"price_id":"price_test","success_url":"http://ok","cancel_url":"http://cancel","metadata":{"tenant_id":"abc"}}`
	req := httptest.NewRequest("POST", "/v1/checkout/sessions", strings.NewReader(body))
	req.Header.Set("Content-Type", "application/json")
	w := httptest.NewRecorder()

	handleCreateCheckoutSession(w, req)

	if w.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", w.Code)
	}

	var session CheckoutSession
	if err := json.NewDecoder(w.Body).Decode(&session); err != nil {
		t.Fatalf("failed to decode response: %v", err)
	}

	if session.Object != "checkout.session" {
		t.Errorf("expected object=checkout.session, got %s", session.Object)
	}
	if !strings.HasPrefix(session.ID, "cs_test_") {
		t.Errorf("expected id prefix cs_test_, got %s", session.ID)
	}
	if session.Metadata["tenant_id"] != "abc" {
		t.Errorf("expected metadata tenant_id=abc, got %s", session.Metadata["tenant_id"])
	}
}

func TestHandleCreateCheckoutSession_UsesExplicitAmount(t *testing.T) {
	body := `{"price_id":"price_test","amount":1495,"currency":"BRL","success_url":"http://ok","cancel_url":"http://cancel","metadata":{"tenant_id":"abc"}}`
	req := httptest.NewRequest("POST", "/v1/checkout/sessions", strings.NewReader(body))
	req.Header.Set("Content-Type", "application/json")
	w := httptest.NewRecorder()

	handleCreateCheckoutSession(w, req)

	if w.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", w.Code)
	}

	var session CheckoutSession
	if err := json.NewDecoder(w.Body).Decode(&session); err != nil {
		t.Fatalf("failed to decode response: %v", err)
	}

	if session.AmountTotal != 1495 {
		t.Fatalf("expected amount_total=1495, got %d", session.AmountTotal)
	}

	if session.Currency != "brl" {
		t.Fatalf("expected currency=brl, got %s", session.Currency)
	}
}

func TestHandleCreateCheckoutSession_InvalidJSON(t *testing.T) {
	req := httptest.NewRequest("POST", "/v1/checkout/sessions", strings.NewReader("not json"))
	w := httptest.NewRecorder()

	handleCreateCheckoutSession(w, req)

	if w.Code != http.StatusBadRequest {
		t.Fatalf("expected 400, got %d", w.Code)
	}
}

func TestComputeStripeSignature(t *testing.T) {
	webhookSecret = "test_secret"
	timestamp := "1234567890"
	payload := []byte(`{"type":"test"}`)

	sig := computeStripeSignature(timestamp, payload)

	signedPayload := fmt.Sprintf("%s.%s", timestamp, string(payload))
	mac := hmac.New(sha256.New, []byte("test_secret"))
	mac.Write([]byte(signedPayload))
	expected := hex.EncodeToString(mac.Sum(nil))

	if sig != expected {
		t.Errorf("signature mismatch: got %s, want %s", sig, expected)
	}
}

func TestCoalesce(t *testing.T) {
	tests := []struct {
		name   string
		values []string
		want   string
	}{
		{name: "returns first non-empty", values: []string{"", "payment", "fallback"}, want: "payment"},
		{name: "returns empty when all empty", values: []string{"", ""}, want: ""},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			if got := coalesce(tt.values...); got != tt.want {
				t.Fatalf("expected %q, got %q", tt.want, got)
			}
		})
	}
}

func TestGetEnv(t *testing.T) {
	const key = "PAYMENT_SIM_TEST_ENV"
	t.Setenv(key, "configured")

	if got := getEnv(key, "fallback"); got != "configured" {
		t.Fatalf("expected configured value, got %q", got)
	}

	os.Unsetenv(key)
	if got := getEnv(key, "fallback"); got != "fallback" {
		t.Fatalf("expected fallback value, got %q", got)
	}
}

func TestGetEnvFloat(t *testing.T) {
	const key = "PAYMENT_SIM_TEST_FLOAT"

	t.Setenv(key, "0.75")
	if got := getEnvFloat(key, 0.5); got != 0.75 {
		t.Fatalf("expected parsed float 0.75, got %f", got)
	}

	t.Setenv(key, "not-a-number")
	if got := getEnvFloat(key, 0.5); got != 0.5 {
		t.Fatalf("expected fallback float 0.5, got %f", got)
	}
}
