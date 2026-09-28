# 🚀 Unity In-Game Carousel LiveOps Web Dashboard

A complete, zero-maintenance web dashboard and REST API to manage and update your Unity Main Menu Carousel slides in real time without needing to build or recompile your game.

---

## 🌟 Features

- **🎮 Live In-Game Simulation:** Interactive 16:9 preview replicating Unity's exact news carousel card, pagination dots, and badges in real-time as you edit.
- **🖼️ Custom Banner Image Uploads:** Drag-and-drop or select JPG, PNG, or WEBP banner images with instant thumbnail previews.
- **🎨 Procedural Gradients:** Built-in color pickers and presets (Cosmic Violet, Cyan Plasma, Solar Ember, Emerald Glow) if you don't have an image.
- **🔗 Click Actions:** Assign action URLs (`https://store.steampowered.com/...`, discord links, or event pages) that open in the player's web browser when they click the slide in Unity.
- **📑 Slide Management:** Add new slides, edit existing ones, reorder slides with Up/Down buttons, and delete slides.
- **⚡ Instant Sync:** One-click **"Publish to Game"** updates the JSON feed. The Unity game loads updates on startup or periodically in the background.
- **🛡️ Offline & Error Resilient:** If the server is offline or unreachable, Unity gracefully falls back to local slides without crashing.

---

## 🚀 Quick Start

### 1. Launch the Dashboard Server
You can simply double-click `start.bat` on Windows, or run from the command line:

```bash
cd web-dashboard
npm start
```

Once running:
- **Web Dashboard (Admin UI):** [http://localhost:3000](http://localhost:3000)
- **Unity API Feed:** [http://localhost:3000/api/carousel](http://localhost:3000/api/carousel)

---

## 🕹️ Unity Setup

1. Open your Unity project (`Billboard`).
2. If you haven't created the Main Menu scene yet, click:
   **`Tools > Billboard > Create 2D Main Menu Scene`** in the top menu bar.
3. Select the **`NewsCard`** GameObject in the hierarchy.
4. In the Inspector, the **`News Carousel`** component has:
   - **`Fetch Remote On Start`**: Checked (`true`)
   - **`Remote Api Url`**: `http://localhost:3000/api/carousel`
   - **`Auto Refresh Interval`**: Set to `0` to fetch once on scene start, or set e.g. `15` to poll every 15 seconds for live in-game updates while players sit in the menu!
5. Hit **Play** in Unity! The carousel will fetch slides directly from your web dashboard.

---

## 🌐 Deploying to the Cloud for Production

When you are ready to distribute your game to real players:
1. Deploy this `web-dashboard` folder to any Node.js hosting platform (e.g., [Render](https://render.com), [Railway](https://railway.app), [Fly.io](https://fly.io), or a VPS like DigitalOcean/AWS).
2. Change the `Remote Api Url` on the `News Carousel` component in Unity from `http://localhost:3000/api/carousel` to your live public URL (e.g. `https://my-game-news.onrender.com/api/carousel`).
3. Now whenever you edit and publish slides on your website, all players across the world see the new carousel content instantly!
