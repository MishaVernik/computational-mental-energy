# CMEflow -- Product & Marketing Strategy

**Mission:** Make mental energy measurable. The calorie counter for the brain.

**Moat:** Only product quantifying mental energy in absolute units (Vernik) with quantum-enhanced flow detection. Patented.

---

## What Exists Today

| Component | Status | Production-ready? |
|-----------|--------|-------------------|
| CmeSim.Api (C# backend) | Working | No -- no auth, secrets in config, no HTTPS, open CORS |
| qbackend (quantum inference) | Working | Partial -- env-based IBM creds, but hardcoded key in test file |
| flow-classifier (classical ML) | Working | Partial -- no auth, loads pickle, heuristic fallback |
| cme-live-dashboard (React) | Working | No -- localStorage "auth", hardcoded localhost URLs |
| muse-bridge (OSC relay) | Working | No -- requires MindMonitor ($15 3rd-party app) |
| Landing page (cmeflow.com) | Built | No -- local only, waitlist exposes emails |
| Database (Azure SQL) | Working | Partial -- no user tables, no retention policy |
| Mobile app | Does not exist | -- |
| Deployment/CI | Does not exist | -- |
| Billing | Does not exist | -- |

---

## What to Do and Why

Each action below closes a specific gap between "lab prototype" and "product people pay for."

### 1. Own the Device Connection (Mobile App)

**Why:** Today a user must buy MindMonitor ($15), configure OSC IP/port manually, and run the bridge on a PC. This is a researcher workflow, not a product. The app IS the product -- it is the first and last thing users interact with.

**What to build (React Native, cross-platform):**

| Screen | Purpose |
|--------|---------|
| Device scan + pair | One-tap BLE connection to Muse. Shows electrode contact quality in real time. Replaces the entire MindMonitor + bridge.py chain. |
| Live session | Real-time band power bars, CME rate gauge, flow indicator, session timer. This is the "wow" moment -- users see their brain activity translated into energy units live. |
| CME dashboard | Today's total Vn, activity breakdown, daily budget bar ("62% used"), weekly trend. The daily view users return to every morning. |
| Session history | Past sessions with CME totals, flow %, activity tags. Proves long-term value: "you burned 23% more energy this week." |
| Measurement protocol | Guided 8-activity calibration (already built on web -- port to mobile). Audio cues, auto-advance. Needed for personalized baselines. |
| Settings | Account, server URL, notification prefs, subscription management. |
| Onboarding | 3-slide intro: what is CME, connect Muse, start tracking. Reduces time-to-value to under 60 seconds. |

**Technical approach:**
- `react-native-ble-plx` for BLE (114K weekly npm downloads, mature)
- Port Muse GATT protocol from `muse-js` (service `0000fe8d`, characteristics `273e0003-0006`, 12 samples/packet at 256 Hz)
- Port `bridge.py` signal processing to TypeScript: Welch PSD, 5-second windows, band power extraction, artifact rejection
- SignalR client (`@microsoft/signalr`) streams windows to backend, receives CME results
- Offline: buffer in local SQLite, sync when connected

### 2. Let Users Be Users (Auth + Accounts)

**Why:** Right now anyone who knows the URL can see everyone's EEG data. There are no users, no sessions tied to people, no privacy. No one will put their brain data into a system without identity and access control.

**What to build:**
- OAuth sign-in (Google + Apple for mobile, email fallback)
- JWT tokens for REST, access token for SignalR
- Per-user data scoping (users only see their own sessions)
- Admin role for waitlist management and system monitoring
- `[Authorize]` on all controllers except health check and waitlist signup POST

### 3. Stop Leaking Secrets (Security)

**Why:** The Azure SQL password is in `appsettings.json`. An IBM Quantum API key is hardcoded in `test_ibm_connection.py`. The waitlist GET endpoint exposes all signed-up emails to anyone. These must be fixed before any public deployment.

**What to do:**
- Move all secrets to environment variables (or Azure Key Vault)
- Add `qbackend/.env` to `.gitignore`
- Delete hardcoded IBM key from `test_ibm_connection.py`
- HTTPS everywhere, HSTS, CORS locked to production domains
- Rate limiting on API endpoints
- Global exception handler (no stack traces in production)
- `EnableDetailedErrors = false` for SignalR in production

### 4. Ship It (Deployment + CI/CD)

**Why:** The product runs on `localhost` via a PowerShell script. No user outside the developer's machine can access it. Deployment is the difference between a project and a product.

**What to build:**
- Fix CmeSim.Api Dockerfile (`Cme.Core` path currently references `archive/Cme.Core` which Docker context can't reach)
- Create Dockerfile for flow-classifier
- Production `docker-compose.yml`: Nginx (TLS via Let's Encrypt), CmeSim.Api, qbackend, flow-classifier
- Static build of cme-live-dashboard served via Nginx at `cmeflow.com`
- API served at `api.cmeflow.com`
- GitHub Actions: build containers, run tests, deploy on push to main
- Hosting: single VPS (Hetzner ~$10/mo) is sufficient for first 500 users

### 5. Get Paid (Billing)

**Why:** Without billing, there is no business. Even with 1,000 users, revenue is zero.

**Pricing (matches current landing page):**

| Tier | Price | Includes |
|------|-------|----------|
| Free | $0 | 3 sessions/week, basic CME dashboard, 7-day history |
| Personal | $9.99/mo ($79/yr) | Unlimited sessions, full activity tracking, daily & weekly reports, energy budget alerts, unlimited history |
| Pro | $19.99/mo ($149/yr) | Everything in Personal + quantum-enhanced flow detection, API access & data export, personalized calibration, adaptive scheduling AI |
| Teams | $49/mo (up to 10) | Anonymized dashboards, burnout alerts, SSO |

**Implementation:** RevenueCat for mobile subscriptions (handles both App Store and Google Play). Stripe for web. Entitlement middleware checks tier before gating features (quantum inference, API access, history depth).

### 6. Respect the Data (Privacy + Compliance)

**Why:** EEG data is biometric and sensitive. GDPR applies to EU users. App Store and Google Play both require privacy nutrition labels and health data disclosures. Getting this wrong means rejection or legal exposure.

**What to build:**
- Consent flow at first launch ("CMEflow processes your EEG data to compute mental energy. Your data is encrypted and never shared.")
- Data export endpoint (download all your data as JSON)
- Data delete endpoint (right to be forgotten)
- Retention policy: auto-purge raw EEG windows after 90 days, keep aggregated CME totals forever
- Privacy policy and terms of service pages

---

## Optional 3rd-Party Integrations

These expand the product surface but are not launch blockers.

### Emotiv EEG Headsets

**Why:** Muse has ~200K active users. Emotiv (EPOC X, Insight 2.0) has a research and enterprise user base. Supporting both triples the addressable market.

**How:** Emotiv Cortex API (WebSocket + JSON). Add an `EmotivSource` adapter in the mobile app alongside `MuseBLE`. Cortex provides raw EEG and band powers -- normalize to the same format our pipeline expects. Different electrode layout (14-channel vs 4-channel) requires recalibrating the flow classifier.

**Effort:** Medium. Requires Emotiv Developer Program registration.

### PLAUD NotePin (Voice Activity Detection)

**Why:** Manual activity tagging is friction. If we can passively detect what the user is doing (meeting, solo work, conversation), CME insights become automatic.

**How:** PLAUD NotePin ($199 wearable recorder) has a REST API. Pull transcription summaries, classify activity type, correlate with CME windows. "Your 45-min standup consumed 8,200 Vn."

**Effort:** Low-medium. Privacy considerations (recording consent).

### Calendar + Productivity Tools

**Why:** Most knowledge workers already have their day structured in a calendar. Auto-tagging activity from calendar events eliminates manual effort entirely.

**How:** Google Calendar / Outlook OAuth. Map event titles to activity types ("Sprint Planning" = `c=0.6`, "Deep Work block" = `c=0.7`). Compare planned vs actual energy spend.

**Effort:** Low. Standard OAuth + REST.

### Wearable Companions (Apple Watch / Wear OS)

**Why:** Users want a glance at their energy level without pulling out their phone.

**How:** Companion app showing current CME level, flow state, and "energy remaining" estimate. Haptic alert when energy drops below threshold.

**Effort:** Medium. Separate watchOS/Wear OS builds.

---

## Marketing Strategy

### The Hardware Problem

CMEflow requires an EEG headband. Users fall into two groups with completely different acquisition paths:

**Group A -- Existing Muse owners (primary target, months 1-6)**

Interaxon (Muse) has generated 6 million hours of meditation data and holds the largest share of the $281M brain-sensing headband market. Estimated installed base: 200K-500K devices worldwide. These users already own the hardware -- they just need our app.

| Muse Model | Price | CMEflow compatible? |
|------------|-------|-------------------|
| Muse 2 | $249.99 | Yes -- 4-channel EEG, BLE, same GATT protocol |
| Muse S (Gen 2) | ~$399 | Yes -- same EEG + sleep sensors |
| Muse S Athena | $474.99 | Yes -- EEG + fNIRS (best model) |

**Group A conversion path:** App Store search / Reddit / word-of-mouth -> install CMEflow (free) -> pair Muse -> immediate value. Zero hardware cost. This is the path to first 100-500 users.

**Group B -- New users who don't own an EEG device**

These users must spend $250-475 on a Muse before they can use CMEflow. This is a significant barrier. The total cost (Muse + CMEflow Pro) ranges from $260-485 in Year 1.

**Strategies to lower the barrier for Group B:**

1. **Muse Affiliate Program** -- Interaxon offers commission-tracked affiliate links. Every CMEflow signup that buys a Muse through our link earns affiliate revenue. Win-win: we lower perceived friction ("here's exactly which device to buy"), Muse gets sales.

2. **Muse Partnership** -- Apply to Muse's Developer/Research partnership program. Possible outcomes:
   - Featured in Muse's app ecosystem as a recommended 3rd-party app
   - Co-marketing: "Muse + CMEflow" bundle on choosemuse.com
   - Discounted Muse devices for CMEflow subscribers
   - SDK commercial license for our app
   - Note: partnership applications are reviewed quarterly

3. **"Works with Muse 2" entry point** -- Market the $249.99 Muse 2 as the minimum viable device, not the $475 Athena. Most users don't need fNIRS. Messaging: "Start tracking your brain energy for $250."

4. **Multi-device support (Phase 5)** -- Adding Emotiv Insight 2.0, FocusCalm, or other cheaper EEG devices opens alternative hardware paths. Long-term, a white-label CMEflow headband ($99-149 target) would eliminate the dependency entirely.

5. **Loaner / trial program** -- For enterprise pilots and university labs, provide loaner Muse devices. Cost: $250-475 per device, recoverable when returned.

### Positioning

**One-liner:** "The calorie counter for your brain."

**Elevator pitch:** CMEflow measures your mental energy in real-time using an EEG headband. It tells you how much brain energy each task costs, when you're in flow, and when to take a break -- backed by patented quantum computing technology and peer-reviewed research.

**Key differentiators to always lead with:**
1. Absolute units (Vernik) -- not relative scores, not percentages, a real measurement
2. Patent-protected quantum flow detection -- no competitor has this
3. Peer-reviewed (ICCSEEA 2026 paper) -- not marketing claims, published science
4. Works with Muse headbands you may already own -- no proprietary hardware

### Target Audiences (in priority order)

| # | Audience | Has Muse? | Pain point | Hook | Channel |
|---|----------|-----------|-----------|------|---------|
| 1 | **Muse owners (meditation users)** | Yes | "My Muse only does meditation, I want more" | Turn your Muse into a productivity tool. No new hardware needed. | Reddit r/Muse, Muse forums, App Store search "muse eeg" |
| 2 | **Quantified-self enthusiasts** | Some | "I track everything except my brain" | Finally, a brain metric that means something | Reddit r/QuantifiedSelf, r/Nootropics, r/Biohackers |
| 3 | **Developers / knowledge workers** | No | "I feel drained by 2pm but don't know why" | See exactly which tasks burn your brain | Twitter/X, Hacker News, Reddit r/productivity |
| 4 | **Researchers / neuroscience** | Often | "I need real-time EEG + cognitive metrics for studies" | Validated pipeline, open API, published methodology | LinkedIn, academic lists, conferences |
| 5 | **Students** | No | "I want to study smarter, not harder" | Find your peak hours, optimize study sessions | TikTok, Instagram, university partnerships |
| 6 | **Corporate wellness / HR** | No | "We need to prevent burnout without surveillance" | Anonymized team energy dashboards | LinkedIn, direct outreach, conferences |

Note: Audiences 1-2 have the shortest path to conversion (already own or are predisposed to buy EEG hardware). Audiences 3-6 require convincing them to buy a Muse first -- content must justify the $250+ investment.

### Acquisition Funnels

**Funnel A: Existing Muse Owner**
```
Discovers CMEflow (App Store / Reddit / article)
  -> Installs free app
  -> Pairs Muse in 30 seconds
  -> Sees live CME data (wow moment)
  -> Uses for 1 week (free tier: 3 sessions/week)
  -> Hits limit, upgrades to Personal ($9.99/mo)
```
CAC: ~$0 (organic). Conversion driver: immediate value with zero friction.

**Funnel B: New User (no device)**
```
Sees content (YouTube / Twitter / HN post)
  -> Visits cmeflow.com landing page
  -> Watches demo video (60s)
  -> Reads "Which device do I need?" guide
  -> Clicks affiliate link to buy Muse 2 ($249.99)
  -> Receives Muse, installs CMEflow
  -> Same path as Funnel A from here
```
CAC: $5-15 (content creation cost amortized). Conversion driver: compelling content that justifies hardware investment. Affiliate commission offsets marketing spend.

**Funnel C: Enterprise / Research**
```
Sees paper or conference talk
  -> Contacts via landing page
  -> Demo call with loaner device
  -> Pilot (5-10 users, 1 month)
  -> Converts to Team tier ($49/mo per 10 users)
```
CAC: $50-100 (time + loaner device logistics). Conversion driver: data and ROI proof.

### Muse Partnership Approach

**Step 1 (immediate):** Join Muse Affiliate Program. Add affiliate links to landing page ("Get a Muse headband" button). Start earning commission on referred hardware sales.

**Step 2 (month 3):** Apply to Muse Developer Partnership. Pitch: "CMEflow extends the Muse value proposition from meditation into productivity. Our users buy Muse devices. We drive hardware sales."

**Step 3 (month 6, if traction):** Propose co-marketing bundle. "Buy Muse S Athena + 1 year CMEflow Pro for $499" on choosemuse.com. Muse sells more devices, we get guaranteed subscribers.

**Step 4 (month 12, if significant traction):** Explore exclusive integration or acquisition conversation. CMEflow's patented CME algorithm becomes a feature inside the official Muse app.

### Launch Plan

**Pre-launch (during development):**
- Build in public on Twitter/X -- weekly threads showing real EEG data, app screenshots, quantum circuits
- Collect waitlist signups via cmeflow.com (already built)
- Post in r/QuantifiedSelf and r/Muse with early demo videos -- test messaging with existing Muse owners
- Write 2-3 blog posts: "How I measured my brain's calorie burn", "What happens to your brain during a Zoom call", "I found my flow state with quantum computing"
- Join Muse Affiliate Program, add affiliate links to landing page

**Launch day:**
- Product Hunt: "CMEflow -- The calorie counter for your brain"
  - Maker story: PhD researcher + quantum computing + real neuroscience
  - Demo video: 60s showing Muse connect -> live EEG -> CME tracking -> daily budget
- Hacker News Show HN: "I built a quantum-enhanced mental energy tracker"
  - Lead with the science, link to paper, show real data
  - The quantum angle will drive discussion (even skeptics generate visibility)
- Cross-post to r/Muse: "I built a free app that turns your Muse into a brain energy tracker"

**Post-launch (ongoing):**
- **Content:** "I tracked my brain for 30 days" YouTube video, weekly Twitter threads with personal CME data
- **Community:** Discord server for users to share CME data, compare activity patterns, suggest features
- **Device guide:** "Which EEG headband should I buy for CMEflow?" blog post / landing page section. Honest comparison, affiliate links. Updated as we add device support.
- **Partnerships:** Reach out to productivity YouTubers (Ali Abdaal, Thomas Frank, Matt D'Avella) for reviews
- **Academic:** Free Pro for 10 university labs using Muse. Their published results become marketing.
- **Referral:** "Invite a friend, both get 1 month Pro free" -- built into the app. For Group B users, "Invite a friend who buys a Muse through your link, get 3 months Pro free."

### Content Calendar (first 3 months post-launch)

| Week | Content | Target Group | Channel |
|------|---------|-------------|---------|
| 1 | Launch post + demo video | A + B | Product Hunt, HN, Twitter, Reddit |
| 2 | "How I measured my brain's calorie burn" blog | B | Blog, LinkedIn, Twitter |
| 3 | "I already own a Muse -- here's what CMEflow adds" | A | Reddit r/Muse, Muse forums |
| 4 | "Coding vs Meetings: which drains your brain more?" | B | Reddit, LinkedIn, YouTube short |
| 5 | "Which EEG headband should I buy?" device guide | B | Blog, SEO, affiliate links |
| 6 | "I tracked my brain during finals week" (student collab) | B | TikTok, Instagram, Reddit |
| 7 | Comparison: CMEflow vs Pylot vs Muse app | A + B | Blog, Reddit r/QuantifiedSelf |
| 8 | Partner podcast appearance | B | YouTube, podcast platforms |
| 9 | "Our first 100 users: aggregate brain energy patterns" | A + B | Blog, Twitter, LinkedIn |
| 10 | Feature announcement: AI energy predictions | A | Product Hunt update, Twitter |
| 11 | Research lab case study | Enterprise | LinkedIn, academic channels |
| 12 | Quarterly review + roadmap share | A + B | Blog, Discord, Twitter |

### Metrics to Track

| Metric | Target (Month 6) | Target (Month 12) |
|--------|-------------------|-------------------|
| Waitlist signups | 500 | 2,000 |
| App installs | 200 | 1,000 |
| Active users (weekly) | 50-100 | 300-500 |
| Paying users | 10-20 | 100-200 |
| MRR | $100-200 | $1,000-2,000 |
| Muse affiliate referrals | 10-20 | 50-100 |
| Affiliate revenue | $50-100 | $250-500 |
| Churn (monthly) | <15% | <10% |
| App Store rating | 4.0+ | 4.5+ |
| NPS | 30+ | 50+ |

---

## Roadmap

### Phase 1 -- Foundation (Months 1-2)

**Goal:** Make the system deployable and secure.

- [ ] Initialize git repo, push to GitHub
- [ ] Remove all hardcoded secrets (SQL password, IBM key)
- [ ] Add OAuth + JWT auth, protect all endpoints
- [ ] Fix Dockerfiles, create production docker-compose.yml
- [ ] Deploy backend to VPS, configure DNS (api.cmeflow.com, cmeflow.com)
- [ ] CI/CD: GitHub Actions build + deploy pipeline
- [ ] React Native project: Expo + react-native-ble-plx + navigation
- [ ] Muse BLE service: scan, connect, parse EEG GATT characteristics
- [ ] EEG processor: port Welch PSD + windowing to TypeScript
- [ ] SignalR mobile client: stream windows, receive CME results

### Phase 2 -- Complete App (Months 3-4)

**Goal:** A polished app that replaces MindMonitor entirely.

- [ ] All app screens: scan, live, dashboard, history, protocol, settings
- [ ] Onboarding flow (3-slide intro)
- [ ] Offline buffering (SQLite) + background sync
- [ ] BLE reconnection handling + battery optimization
- [ ] Daily CME budget view + energy forecast
- [ ] Push notifications (session reminders, daily summary)
- [ ] App icon, splash screen, app store screenshots
- [ ] TestFlight (iOS) + Google Play Internal Testing (Android)
- [ ] Begin building-in-public on Twitter/X

### Phase 3 -- Launch + Monetize (Months 5-6)

**Goal:** Public launch, first paying users.

- [ ] RevenueCat subscription integration (Free/Personal/Pro)
- [ ] GDPR consent flow + data export/delete
- [ ] Privacy policy + terms of service
- [ ] App Store + Google Play public listing
- [ ] Landing page updated with download links
- [ ] ICCSEEA paper published
- [ ] Product Hunt + Hacker News launch
- [ ] Reddit, Twitter/X, YouTube marketing push
- [ ] Discord community server

### Phase 4 -- Intelligence + Growth (Months 7-9)

**Goal:** Retain users, grow through word-of-mouth.

- [ ] AI insights: "You have 2h of deep work left", weekly energy reports
- [ ] Shareable CME summaries ("I used 4,200 Vn today")
- [ ] Apple Watch / Wear OS companion
- [ ] Referral program (invite = 1 month Pro free)
- [ ] Calendar sync (Google Calendar / Outlook) for auto activity tagging
- [ ] Content marketing: blog, LinkedIn, podcaster outreach
- [ ] University lab partnerships (free Pro for researchers)
- [ ] "I tracked my brain for 30 days" YouTube video

### Phase 5 -- Expansion (Months 10-12)

**Goal:** Expand market, validate enterprise, explore fundraising.

- [ ] Emotiv headset support (Cortex API)
- [ ] PLAUD NotePin integration (voice activity correlation)
- [ ] Team tier: team dashboard, aggregate analytics, admin panel
- [ ] Public REST API for developers
- [ ] Enterprise pilots (2-3 companies)
- [ ] Case studies from power users
- [ ] Accelerator applications (YC, Techstars) if traction warrants
- [ ] Evaluate building proprietary CMEflow hardware (EEG band optimized for CME)

---

## Revenue Projection

| Month | Active users | Paying | MRR | Cumulative revenue |
|-------|-------------|--------|-----|-------------------|
| 3 | 10 | 0 | $0 | $0 |
| 6 | 50-100 | 10-20 | $100-200 | $300-600 |
| 9 | 100-300 | 50-100 | $500-1,000 | $2,000-4,000 |
| 12 | 300-500 | 100-200 | $1,000-2,000 | $6,500-13,000 |

**Monthly costs:** VPS $10-40, Azure SQL $5-15, Apple Dev $8, Google Play $2 (amortized), domain $1 = **~$25-65/mo**

**Break-even:** ~7 paying Personal users covers all infrastructure.

---

## Competitive Position

| Competitor | What they do | CMEflow advantage |
|------------|-------------|-------------------|
| Pylot ($299 device) | Mental energy, proprietary hardware | Works with existing Muse, no new hardware |
| Niura (earbuds) | Focus tracking | Absolute energy units (Vernik), not relative scores |
| Aora ("Whoop for brain") | Cognitive monitoring, beta 2026 | Shipping first, quantum flow detection, patent, published research |
| Muse app | Meditation only | Productivity + energy tracking, not just meditation |
| CortQ | Enterprise cognitive load via Slack/Jira | Real EEG data, individual-first, no surveillance optics |

---

## Key Risks

| Risk | Mitigation |
|------|------------|
| **Hardware barrier** ($250-475 to start) | Lead with existing Muse owners (Group A, zero cost). Affiliate links for Group B. Device guide showing cheapest path ($249.99 Muse 2). Long-term: own hardware. |
| Muse BLE protocol changes | Abstract device layer; `muse-js` community maintains spec |
| Muse blocks 3rd-party apps | Unlikely (open SDK + partner program). Fallback: Emotiv or direct BLE reverse-engineering. |
| App Store medical device rejection | Position as wellness/productivity; health disclaimers |
| Small total addressable market | Muse installed base ~200-500K. Adding Emotiv, FocusCalm, Neurosity expands 3-5x. Own hardware removes ceiling entirely. |
| Solo developer bottleneck | Minimal scope per phase; automate everything; weekly ship cadence |
| Quantum costs on real QPU | Aer simulator for Free/Personal; real QPU as Pro-only premium feature |
| Data privacy (EEG = biometric) | GDPR flow, encryption at rest, auto-purge raw data, consent-first design |
| Competitors ship faster | Lead with science credibility (patent + paper); no one else has Vernik units |
