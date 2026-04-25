.DEFAULT_GOAL := help

JUST ?= just
REGISTRY ?= registry.digitalocean.com/consultores
TAG ?= latest
OVERLAY ?= dev
NAMESPACE ?= consultores

PASS_ENV = REGISTRY="$(REGISTRY)" TAG="$(TAG)" SLUGS="$(SLUGS)" SECRETS_FILE="$(SECRETS_FILE)" OVERLAY="$(OVERLAY)" NAMESPACE="$(NAMESPACE)" APP="$(APP)" KIND="$(KIND)" NAME="$(NAME)" CONFIGURATION="$(CONFIGURATION)"

JUST_TARGETS := \
	setup setup-hosts stripe-webhook \
	dev up down stop destroy status logs build \
	k8s-dev-up k8s-dev-start k8s-dev-stop k8s-dev-down k8s-dev-status k8s-dev-logs \
	k8s-dev-preview k8s-dev-apply k8s-apply k8s-diff k8s-apply-staging k8s-apply-prod k8s-secrets \
	k8s-resources k8s-events k8s-describe k8s-logs k8s-redis-port-forward \
	k8s-headlamp-install k8s-headlamp-port-forward k8s-headlamp-token \
	k8s-redisinsight-port-forward \
	tf-cluster-init tf-cluster-plan tf-cluster-apply tf-cluster-destroy tf-cluster-output \
	tf-platform-init tf-platform-plan tf-platform-apply tf-platform-destroy \
	tf-apply-all tf-destroy-all tf-init tf-plan tf-apply tf-destroy tf-output \
	talos-kubeconfig talosconfig \
	image-build image-push docker-build-prod docker-push docker-build-staging docker-push-staging

.PHONY: help $(JUST_TARGETS)

define require_just
	@command -v $(JUST) >/dev/null 2>&1 || { \
		echo "ERROR: 'just' is required for repo tasks."; \
		echo "Install it with: brew install just"; \
		exit 127; \
	}
endef

help:
	$(require_just)
	@$(JUST) --list

$(JUST_TARGETS):
	$(require_just)
	@$(PASS_ENV) $(JUST) $@
