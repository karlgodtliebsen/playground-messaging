#!/bin/bash

echo "Deploying messaging system to k3d..."

# Apply manifests in order
kubectl apply -f 01-namespace.yaml
echo "Waiting for namespace..."
sleep 2

kubectl apply -f 02-kafka-minimal.yaml
echo "Kafka deployed..."
echo "Waiting for Kafka to start (this will take 60-90 seconds on your hardware)..."
sleep 90

kubectl apply -f 03-seq.yaml
echo "Seq deployed..."
sleep 10

kubectl apply -f 04-redpanda-console.yaml
echo "RedPanda Console deployed..."
sleep 10

kubectl apply -f 05-producer.yaml
echo "Producer deployed..."
sleep 5

kubectl apply -f 06-consumer.yaml
echo "Consumer deployed..."

sleep 5

kubectl apply -f 07-ingress.yaml
echo "Ingress Resources deployed..."

sleep 5


echo ""
echo "Deployment complete! Waiting for pods to start..."
echo ""

sleep 10

kubectl get pods -n messaging

echo ""
echo "=== Access URLs ==="
echo "RedPanda Console: http://localhost:9080"
echo "Seq:              http://localhost:9341"
echo ""
echo "=== Useful Commands ==="
echo "Watch pods:       kubectl get pods -n messaging -w"
echo "Producer logs:    kubectl logs -n messaging -l app=producer -f"
echo "Consumer logs:    kubectl logs -n messaging -l app=consumer -f"
echo "All services:     kubectl get all -n messaging"