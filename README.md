# Mobile Game Architecture & Core Systems (Unity / C#)

Production-ready gameplay systems, monetization architecture, and native mobile bridges engineered for commercial mobile puzzle titles (including *Boom Blast Block Puzzle* on Google Play Store).

---

## 🛠 Tech Stack & Environment
* **Engine**: Unity 2D/3D (C#)
* **Target OS**: Android (API 33+)
* **Monetization**: Google Mobile Ads SDK (AdMob)
* **Optimization**: Target 60 FPS, Low Memory Footprint, Zero Multi-Touch Conflicts

---

## 🧩 Architectural Highlights

### 1. Dynamic Matrix & Placement Logic (Grid Engine)
* Real-time grid bounds detection and cell state matrix indexing.
* Multi-line full-clearing algorithms with streak tracking and harmonic audio pitch escalation.
* Boundary-safe touch drag-and-drop validation preventing multi-touch deadlocks.

### 2. Monetization Pipeline (Google AdMob)
* Fully asynchronous lifecycle wrappers for **Banner**, **Interstitial**, and **Rewarded Video** ads.
* Main-thread safe callback dispatchers to avoid Unity background thread lockups.
* Failover handling and graceful timeouts for offline players.

### 3. Persistent Economy (Heart / Life Refill Engine)
* Unix-epoch binary timestamp logic calculating offline life restoration.
* Anti-tamper local storage guards against basic system clock manipulation.

### 4. Native Android OS Bridge
* Android Java class bindings for device-specific tactile feedback (Haptics/Vibration).
* Hardware back-button (Escape key) stack router ensuring clean navigation.
* Policy-compliant Google Play In-App Review API integration triggered via non-intrusive UI.

---

## 📱 Published Titles
* **Boom Blast Block Puzzle** — Live on Google Play Store
* **Zoo Jam Garden: Match 3** — Pipeline Closed Testing
* **Rotate Jam** — Production Ready
