const express = require('express');
const cors = require('cors');
const path = require('path');
const fs = require('fs');
const multer = require('multer');

const app = express();
const PORT = process.env.PORT || 3000;

// Enable CORS for all routes so Unity editor & standalone builds can fetch slides
app.use(cors());
app.use(express.json());

// Paths
const DATA_DIR = path.join(__dirname, 'data');
const DATA_FILE = path.join(DATA_DIR, 'carousel.json');
const UPLOADS_DIR = path.join(__dirname, 'public', 'uploads');

// Ensure directories exist
if (!fs.existsSync(DATA_DIR)) fs.mkdirSync(DATA_DIR, { recursive: true });
if (!fs.existsSync(UPLOADS_DIR)) fs.mkdirSync(UPLOADS_DIR, { recursive: true });

// Setup Multer for image file uploads
const storage = multer.diskStorage({
  destination: (req, file, cb) => cb(null, UPLOADS_DIR),
  filename: (req, file, cb) => {
    const ext = path.extname(file.originalname).toLowerCase();
    const uniqueSuffix = Date.now() + '-' + Math.round(Math.random() * 1e9);
    cb(null, 'slide-' + uniqueSuffix + ext);
  }
});

const upload = multer({
  storage: storage,
  limits: { fileSize: 10 * 1024 * 1024 }, // 10MB limit
  fileFilter: (req, file, cb) => {
    const allowed = /jpeg|jpg|png|webp|gif/;
    const ext = allowed.test(path.extname(file.originalname).toLowerCase());
    const mime = allowed.test(file.mimetype);
    if (ext && mime) {
      return cb(null, true);
    }
    cb(new Error('Only image files (JPG, PNG, WEBP, GIF) are allowed.'));
  }
});

// Serve static frontend and uploads
app.use(express.static(path.join(__dirname, 'public')));

// Helper to read data
function getCarouselData() {
  if (!fs.existsSync(DATA_FILE)) {
    return { version: 1, updatedAt: new Date().toISOString(), slides: [] };
  }
  try {
    const raw = fs.readFileSync(DATA_FILE, 'utf8');
    return JSON.parse(raw);
  } catch (err) {
    console.error('Error reading carousel.json:', err);
    return { version: 1, updatedAt: new Date().toISOString(), slides: [] };
  }
}

// Helper to save data
function saveCarouselData(data) {
  data.updatedAt = new Date().toISOString();
  fs.writeFileSync(DATA_FILE, JSON.stringify(data, null, 2), 'utf8');
}

// Endpoint: GET /api/carousel (Unity Game Client fetches this)
app.get('/api/carousel', (req, res) => {
  const data = getCarouselData();
  const host = req.get('host');
  const protocol = req.protocol;
  const baseUrl = `${protocol}://${host}`;

  // Normalize image URLs so Unity receives full resolvable URLs
  const slidesWithFullUrls = (data.slides || []).map(slide => {
    let fullImageUrl = slide.imageUrl || '';
    if (fullImageUrl && fullImageUrl.startsWith('/')) {
      fullImageUrl = `${baseUrl}${fullImageUrl}`;
    }
    return {
      ...slide,
      imageUrl: fullImageUrl
    };
  });

  res.json({
    version: data.version || 1,
    updatedAt: data.updatedAt,
    slides: slidesWithFullUrls
  });
});

// Endpoint: POST /api/carousel (Dashboard saves slide changes)
app.post('/api/carousel', (req, res) => {
  try {
    const { slides } = req.body;
    if (!Array.isArray(slides)) {
      return res.status(400).json({ error: 'Payload must contain a "slides" array.' });
    }

    const currentData = getCarouselData();
    const updatedData = {
      version: (currentData.version || 1) + 1,
      slides: slides.map((s, index) => ({
        id: s.id || `slide_${Date.now()}_${index}`,
        title: s.title || 'NEW ANNOUNCEMENT',
        subtitle: s.subtitle || '',
        badge: s.badge || 'NEWS',
        bgGradientStart: s.bgGradientStart || '#240f50',
        bgGradientEnd: s.bgGradientEnd || '#802060',
        imageUrl: s.imageUrl || '',
        actionUrl: s.actionUrl || ''
      }))
    };

    saveCarouselData(updatedData);
    console.log(`[Carousel API] Updated slides (count: ${updatedData.slides.length}, version: ${updatedData.version})`);

    res.json({ success: true, message: 'Carousel saved successfully', data: updatedData });
  } catch (err) {
    console.error('Error saving slides:', err);
    res.status(500).json({ error: 'Failed to save carousel data.' });
  }
});

// Endpoint: POST /api/upload (Dashboard uploads custom banner image)
app.post('/api/upload', upload.single('image'), (req, res) => {
  if (!req.file) {
    return res.status(400).json({ error: 'No image file uploaded.' });
  }

  const relativeUrl = `/uploads/${req.file.filename}`;
  const host = req.get('host');
  const fullUrl = `${req.protocol}://${host}${relativeUrl}`;

  res.json({
    success: true,
    filename: req.file.filename,
    url: relativeUrl,
    fullUrl: fullUrl
  });
});

// Endpoint: GET /api/status (Quick healthcheck)
app.get('/api/status', (req, res) => {
  const data = getCarouselData();
  res.json({
    status: 'online',
    slidesCount: (data.slides || []).length,
    version: data.version || 1,
    updatedAt: data.updatedAt
  });
});

app.listen(PORT, () => {
  console.log('====================================================');
  console.log(`🚀 Carousel LiveOps Web Dashboard running on:`);
  console.log(`   👉 Admin Web UI:      http://localhost:${PORT}`);
  console.log(`   👉 Unity API Feed:    http://localhost:${PORT}/api/carousel`);
  console.log('====================================================');
});
