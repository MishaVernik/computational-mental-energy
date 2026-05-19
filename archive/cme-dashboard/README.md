# CME Dashboard

Modern React web dashboard for the CME Quantum ML System.

## Features

- **Real-time System Monitoring**: View system status, metrics, and performance
- **Online Inference**: Submit EEG data for CME computation
- **Training Job Management**: Start and monitor long-running training jobs
- **Performance Visualization**: Charts showing latency distribution and job status
- **Recent Activity**: Live updates of training job progress

## Tech Stack

- React 18 + TypeScript
- Vite (build tool)
- Recharts (data visualization)
- Lucide React (icons)
- Axios (HTTP client)

## Development

### Prerequisites

- Node.js 20+
- Running backend services (API + Quantum Backend + SQL Server)

### Setup

```bash
npm install
```

### Run Development Server

```bash
npm run dev
```

Dashboard will be available at http://localhost:3000

API requests are proxied to `http://localhost:5000/api` by default.

### Build for Production

```bash
npm run build
npm run preview
```

## Docker

### Build Image

```bash
docker build -t cme-dashboard .
```

### Run Container

```bash
docker run -p 3000:3000 cme-dashboard
```

## Environment Variables

Create `.env.local` for custom configuration:

```env
VITE_API_BASE_URL=http://localhost:5000/api
```

## Usage

### Online Inference

1. Navigate to the "Online Inference" panel
2. Enter Session ID (use existing: `11111111-1111-1111-1111-111111111111`)
3. Adjust task difficulty slider
4. Enter EEG features as JSON array
5. Click "Compute CME"
6. View results including CME value, flow probability, and latency

### Training Jobs

1. Navigate to the "Training Jobs" panel
2. Set number of generations using slider
3. Click "Start Training Job"
4. Job will be queued and processed by background worker
5. Monitor progress in "Recent Training Jobs" table

### Metrics

- **System Overview**: Real-time stats (requests, sessions, response time, jobs)
- **Performance Metrics**: Charts showing latency distribution
- **Recent Activity**: Table of training jobs with live progress bars

## Components

- **App.tsx**: Main application container
- **SystemStatus.tsx**: Overview cards and metrics
- **InferencePanel.tsx**: Online inference form and results
- **TrainingPanel.tsx**: Training job submission form
- **MetricsChart.tsx**: Performance visualization charts
- **RecentActivity.tsx**: Training jobs table with auto-refresh

## API Integration

All API calls go through `src/api/client.ts`:

- `getDashboardSummary()`: Get aggregated metrics
- `submitInference()`: POST inference request
- `startTrainingJob()`: Start new training job
- `listTrainingJobs()`: Get recent jobs

## Styling

- Dark theme optimized for long viewing sessions
- Responsive design (mobile-friendly)
- Custom CSS with modern glassmorphism effects
- Color-coded status badges

## Auto-Refresh

- Dashboard summary: Every 5 seconds
- Training jobs table: Every 10 seconds

## Browser Support

- Chrome/Edge (recommended)
- Firefox
- Safari
- Modern browsers with ES2020 support


