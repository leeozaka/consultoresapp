#!/bin/bash
# Creates the S3 bucket used for property photo uploads in development.
# This script is executed automatically by LocalStack on container start.

echo "Initializing LocalStack S3..."

awslocal s3 mb s3://property-photos 2>/dev/null || true

# Disable Block Public Access — required so bucket policies that allow public
# reads are not silently overridden (default BPA blocks all public policies).
awslocal s3api put-public-access-block \
  --bucket property-photos \
  --public-access-block-configuration \
  'BlockPublicAcls=false,IgnorePublicAcls=false,BlockPublicPolicy=false,RestrictPublicBuckets=false'

awslocal s3api put-bucket-policy --bucket property-photos --policy '{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "PublicReadGetObject",
      "Effect": "Allow",
      "Principal": "*",
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::property-photos/*"
    }
  ]
}'

# CORS — allow the browser (Angular dev server & Docker Caddy) to fetch blobs
# directly from this S3 bucket.  Without this, cross-origin XHR / fetch requests
# from the frontend receive status 0 ("Unknown Error") because the browser blocks
# the response when no Access-Control-Allow-Origin header is present.
awslocal s3api put-bucket-cors --bucket property-photos --cors-configuration '{
  "CORSRules": [
    {
      "AllowedOrigins": ["*"],
      "AllowedMethods": ["GET", "HEAD"],
      "AllowedHeaders": ["*"],
      "ExposeHeaders": ["ETag", "Content-Length", "Content-Type"],
      "MaxAgeSeconds": 3600
    }
  ]
}'

echo "LocalStack S3 bucket 'property-photos' ready."
