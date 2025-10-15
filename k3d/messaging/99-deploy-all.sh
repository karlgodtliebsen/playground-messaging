#!/bin/bash

echo "Deploying messaging system to k3d..."

# Apply manifests in order
kubectl apply -f 01-namespace.yaml  --validate=false
echo "Waiting for namespace..."
sleep 2

kubectl apply -f 02-kafka-minimal.yaml --validate=false
echo "Kafka deployed..."
echo "Waiting for Kafka to start (this will take 60-90 seconds on your hardware)..."
sleep 90

kubectl apply -f 03-seq.yaml --validate=false
echo "Seq deployed..."
sleep 10

kubectl apply -f 04-redpanda-console.yaml --validate=false
echo "RedPanda Console deployed..."
sleep 10

kubectl apply -f 05-producer.yaml --validate=false
echo "Producer deployed..."
sleep 5

kubectl apply -f 06-consumer.yaml --validate=false
echo "Consumer deployed..."

sleep 5

kubectl apply -f 07-ingress.yaml --validate=false
echo "Ingress Resources deployed..."

sleep 5


echo ""
echo "Deployment complete! Waiting for pods to start..."
echo ""

sleep 10

kubectl get pods -n messaging

echo ""
echo "=== Access URLs ==="
echo "=== [After Modifying host files] ==="
echo "RedPanda Console: http://redpanda.local"
echo "Seq:              http://seq.local"
echo ""
echo "=== Useful Commands ==="
echo "Watch pods:       kubectl get pods -n messaging -w"
echo "Producer logs:    kubectl logs -n messaging -l app=producer -f"
echo "Consumer logs:    kubectl logs -n messaging -l app=consumer -f"
echo "Ingress logs:     kubectl logs -n messaging -l app=ingress -f"
echo "Ingress service:  kubectl get ingress -n messaging
echo "All services:     kubectl get all -n messaging"

echo "Stop cluster:     k3d cluster stop messaging"
echo "Start cluster:    k3d cluster start messaging"


