#!/bin/bash

hey -m POST -D ./body.json -H "Content-Type: application/json" -c 100 -n 10000 -h2 https://localhost:8080/api/events
