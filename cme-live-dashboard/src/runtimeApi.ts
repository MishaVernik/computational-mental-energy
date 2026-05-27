/** API / SignalR base URL: localhost dev, :5000 on raw IP:port, same-origin behind IIS HTTPS. */
export function getApiBase(): string {
  const { hostname, protocol, port } = window.location;
  if (hostname === 'localhost' || hostname === '127.0.0.1') {
    return 'http://localhost:5000';
  }
  if (!port || port === '443' || port === '80') {
    return window.location.origin;
  }
  return `http://${hostname}:5000`;
}

export function getHubUrl(): string {
  return `${getApiBase()}/eeg-stream`;
}
