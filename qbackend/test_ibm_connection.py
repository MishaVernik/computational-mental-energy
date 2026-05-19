"""Test IBM Quantum connection and list available backends."""
import os
import certifi
import requests
import json

os.environ['REQUESTS_CA_BUNDLE'] = certifi.where()

TOKEN = 'jxusacA2AQunQyOszIn4HrB5pFSBaRE6qgewh-V1WGbp'
CRN = 'crn:v1:bluemix:public:quantum-computing:us-east:a/805095476bd24c6a9a62b8be74dd4b86:feec4f7a-b5ca-4311-8471-5ecd6aa468c4::'

# Step 1: Get IAM token from API key
print("=== Step 1: IAM Token ===")
iam_resp = requests.post(
    'https://iam.cloud.ibm.com/identity/token',
    data={'grant_type': 'urn:ibm:params:oauth:grant-type:apikey', 'apikey': TOKEN},
    headers={'Content-Type': 'application/x-www-form-urlencoded'},
)
print(f"IAM status: {iam_resp.status_code}")
if iam_resp.status_code != 200:
    print(f"IAM error: {iam_resp.text[:500]}")
    exit(1)

iam_token = iam_resp.json()['access_token']
print(f"Got IAM token: {iam_token[:40]}...")

# Step 2: List backends via REST API
print("\n=== Step 2: List Backends ===")
headers = {
    'Authorization': f'Bearer {iam_token}',
    'Service-CRN': CRN,
}
resp = requests.get('https://us-east.quantum-computing.cloud.ibm.com/backends', headers=headers)
print(f"Backends status: {resp.status_code}")
if resp.status_code == 200:
    data = resp.json()
    backends = data.get('backends', data) if isinstance(data, dict) else data
    if isinstance(backends, list):
        print(f"Found {len(backends)} backends:")
        for b in backends:
            name = b.get('name', b.get('backend_name', '?'))
            nq = b.get('num_qubits', b.get('n_qubits', '?'))
            status = b.get('status', '?')
            print(f"  {name}: {nq} qubits, status={status}")
    else:
        print(json.dumps(data, indent=2)[:1000])
else:
    print(f"Error: {resp.text[:500]}")

# Step 3: Also try eu-de region
print("\n=== Step 3: Try EU-DE region ===")
resp2 = requests.get('https://eu-de.quantum-computing.cloud.ibm.com/backends', headers=headers)
print(f"EU-DE status: {resp2.status_code}")
if resp2.status_code == 200:
    data2 = resp2.json()
    backends2 = data2.get('backends', data2) if isinstance(data2, dict) else data2
    if isinstance(backends2, list):
        print(f"Found {len(backends2)} backends:")
        for b in backends2:
            name = b.get('name', b.get('backend_name', '?'))
            nq = b.get('num_qubits', b.get('n_qubits', '?'))
            print(f"  {name}: {nq} qubits")
    else:
        print(json.dumps(data2, indent=2)[:1000])
else:
    print(f"Error: {resp2.text[:500]}")
