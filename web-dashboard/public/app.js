// State
let slides = [];
let currentSelectedIndex = 0;
let previewIndex = 0;
let autoPreviewInterval = null;

// DOM Elements
const endpointUrlEl = document.getElementById('endpointUrl');
const btnCopyEndpoint = document.getElementById('btnCopyEndpoint');
const btnRefresh = document.getElementById('btnRefresh');
const btnSaveAll = document.getElementById('btnSaveAll');

// Preview Elements
const previewSlide = document.getElementById('previewSlide');
const previewDots = document.getElementById('previewDots');
const previewBadge = document.getElementById('previewBadge');
const previewSubtitle = document.getElementById('previewSubtitle');
const previewTitle = document.getElementById('previewTitle');
const previewCounter = document.getElementById('previewCounter');
const btnPreviewPrev = document.getElementById('btnPreviewPrev');
const btnPreviewNext = document.getElementById('btnPreviewNext');
const prevArrow = document.getElementById('prevArrow');
const nextArrow = document.getElementById('nextArrow');

// Slides List
const slidesList = document.getElementById('slidesList');
const btnAddSlide = document.getElementById('btnAddSlide');

// Editor Elements
const editorHeading = document.getElementById('editorHeading');
const slideIndexBadge = document.getElementById('slideIndexBadge');
const slideEditForm = document.getElementById('slideEditForm');
const slideTitle = document.getElementById('slideTitle');
const slideSubtitle = document.getElementById('slideSubtitle');
const slideBadge = document.getElementById('slideBadge');
const slideActionUrl = document.getElementById('slideActionUrl');
const tabGradient = document.getElementById('tabGradient');
const tabImage = document.getElementById('tabImage');
const sectionGradient = document.getElementById('sectionGradient');
const sectionImage = document.getElementById('sectionImage');
const gradStart = document.getElementById('gradStart');
const gradStartHex = document.getElementById('gradStartHex');
const gradEnd = document.getElementById('gradEnd');
const gradEndHex = document.getElementById('gradEndHex');
const dropzone = document.getElementById('dropzone');
const fileInput = document.getElementById('fileInput');
const dropzoneContent = document.getElementById('dropzoneContent');
const imagePreviewContainer = document.getElementById('imagePreviewContainer');
const imagePreview = document.getElementById('imagePreview');
const btnRemoveImage = document.getElementById('btnRemoveImage');
const slideImageUrl = document.getElementById('slideImageUrl');
const btnDeleteCurrentSlide = document.getElementById('btnDeleteCurrentSlide');
const toastEl = document.getElementById('toast');

// --- Initialization ---
document.addEventListener('DOMContentLoaded', () => {
  // Update endpoint display to current origin
  const currentOrigin = window.location.origin;
  endpointUrlEl.textContent = `${currentOrigin}/api/carousel`;

  bindEvents();
  fetchCarousel();
});

function bindEvents() {
  btnRefresh.addEventListener('click', fetchCarousel);
  btnSaveAll.addEventListener('click', saveAllSlides);
  btnAddSlide.addEventListener('click', handleAddSlide);

  btnCopyEndpoint.addEventListener('click', () => {
    navigator.clipboard.writeText(endpointUrlEl.textContent);
    showToast('Copied endpoint URL to clipboard!', 'success');
  });

  // Preview Nav
  btnPreviewPrev.addEventListener('click', () => stepPreview(-1));
  btnPreviewNext.addEventListener('click', () => stepPreview(1));
  prevArrow.addEventListener('click', () => stepPreview(-1));
  nextArrow.addEventListener('click', () => stepPreview(1));

  // Style Tabs
  tabGradient.addEventListener('click', () => setStyleTab('gradient'));
  tabImage.addEventListener('click', () => setStyleTab('image'));

  // Color inputs sync
  gradStart.addEventListener('input', (e) => {
    gradStartHex.value = e.target.value;
    updateCurrentSlideFromForm();
  });
  gradStartHex.addEventListener('input', (e) => {
    if (/^#[0-9A-Fa-f]{6}$/.test(e.target.value)) {
      gradStart.value = e.target.value;
      updateCurrentSlideFromForm();
    }
  });

  gradEnd.addEventListener('input', (e) => {
    gradEndHex.value = e.target.value;
    updateCurrentSlideFromForm();
  });
  gradEndHex.addEventListener('input', (e) => {
    if (/^#[0-9A-Fa-f]{6}$/.test(e.target.value)) {
      gradEnd.value = e.target.value;
      updateCurrentSlideFromForm();
    }
  });

  // Preset buttons
  document.querySelectorAll('.preset-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      const s = btn.dataset.s;
      const e = btn.dataset.e;
      gradStart.value = s;
      gradStartHex.value = s;
      gradEnd.value = e;
      gradEndHex.value = e;
      updateCurrentSlideFromForm();
    });
  });

  // Form Inputs Live Update
  [slideTitle, slideSubtitle, slideBadge, slideActionUrl, slideImageUrl].forEach(input => {
    input.addEventListener('input', updateCurrentSlideFromForm);
  });

  slideEditForm.addEventListener('submit', (e) => {
    e.preventDefault();
    updateCurrentSlideFromForm();
    showToast('Slide updated locally! Click "Publish to Game" when ready.', 'success');
  });

  btnDeleteCurrentSlide.addEventListener('click', handleDeleteCurrentSlide);

  // Drag and drop image upload
  dropzone.addEventListener('click', (e) => {
    if (e.target !== btnRemoveImage) {
      fileInput.click();
    }
  });

  fileInput.addEventListener('change', handleFileSelect);

  dropzone.addEventListener('dragover', (e) => {
    e.preventDefault();
    dropzone.classList.add('dragover');
  });

  dropzone.addEventListener('dragleave', () => {
    dropzone.classList.remove('dragover');
  });

  dropzone.addEventListener('drop', (e) => {
    e.preventDefault();
    dropzone.classList.remove('dragover');
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      uploadImageFile(e.dataTransfer.files[0]);
    }
  });

  btnRemoveImage.addEventListener('click', (e) => {
    e.stopPropagation();
    slideImageUrl.value = '';
    imagePreview.src = '';
    imagePreviewContainer.classList.add('hidden');
    dropzoneContent.classList.remove('hidden');
    updateCurrentSlideFromForm();
  });
}

// --- Fetch & Save API ---
async function fetchCarousel() {
  try {
    const res = await fetch('/api/carousel');
    if (!res.ok) throw new Error(`HTTP error ${res.status}`);
    const data = await res.json();
    slides = data.slides || [];
    if (slides.length === 0) {
      handleAddSlide();
    } else {
      currentSelectedIndex = 0;
      previewIndex = 0;
      renderAll();
    }
    showToast('Loaded latest slides from server', 'success');
  } catch (err) {
    console.error('Fetch error:', err);
    showToast('Failed to load slides: ' + err.message, 'error');
  }
}

async function saveAllSlides() {
  try {
    btnSaveAll.disabled = true;
    btnSaveAll.textContent = '⏳ Publishing...';

    const res = await fetch('/api/carousel', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ slides })
    });

    if (!res.ok) throw new Error(`Save failed with HTTP ${res.status}`);
    const data = await res.json();

    showToast('🎉 Published to game! Unity will load these slides.', 'success');
  } catch (err) {
    console.error('Save error:', err);
    showToast('Publish failed: ' + err.message, 'error');
  } finally {
    btnSaveAll.disabled = false;
    btnSaveAll.textContent = '💾 Publish to Game';
  }
}

// --- Render Functions ---
function renderAll() {
  renderSlideList();
  renderEditorForm();
  renderPreview();
}

function renderSlideList() {
  slidesList.innerHTML = '';

  slides.forEach((slide, idx) => {
    const item = document.createElement('div');
    item.className = `slide-item ${idx === currentSelectedIndex ? 'active' : ''}`;
    
    // Background thumbnail
    let thumbStyle = '';
    if (slide.imageUrl) {
      thumbStyle = `background-image: url('${slide.imageUrl}');`;
    } else {
      const s = slide.bgGradientStart || '#240f50';
      const e = slide.bgGradientEnd || '#802060';
      thumbStyle = `background: linear-gradient(135deg, ${s}, ${e});`;
    }

    item.innerHTML = `
      <div class="slide-item-thumb" style="${thumbStyle}"></div>
      <div class="slide-item-info">
        <div class="slide-item-title">${escapeHtml(slide.title || 'Untitled Slide')}</div>
        <div class="slide-item-meta">
          <span class="slide-item-badge">${escapeHtml(slide.badge || 'NEWS')}</span>
          <span>${escapeHtml(slide.subtitle || '')}</span>
        </div>
      </div>
      <div class="slide-item-actions">
        <button class="btn-icon" data-action="up" title="Move Up" ${idx === 0 ? 'disabled' : ''}>▲</button>
        <button class="btn-icon" data-action="down" title="Move Down" ${idx === slides.length - 1 ? 'disabled' : ''}>▼</button>
      </div>
    `;

    item.addEventListener('click', (e) => {
      const btn = e.target.closest('button');
      if (btn) {
        e.stopPropagation();
        const action = btn.dataset.action;
        if (action === 'up' && idx > 0) swapSlides(idx, idx - 1);
        if (action === 'down' && idx < slides.length - 1) swapSlides(idx, idx + 1);
        return;
      }
      selectSlide(idx);
    });

    slidesList.appendChild(item);
  });
}

function selectSlide(index) {
  currentSelectedIndex = index;
  previewIndex = index;
  renderAll();
}

function swapSlides(from, to) {
  const temp = slides[from];
  slides[from] = slides[to];
  slides[to] = temp;
  if (currentSelectedIndex === from) currentSelectedIndex = to;
  else if (currentSelectedIndex === to) currentSelectedIndex = from;
  previewIndex = currentSelectedIndex;
  renderAll();
}

function renderEditorForm() {
  if (slides.length === 0) return;
  const slide = slides[currentSelectedIndex];

  editorHeading.textContent = `✏️ Edit Slide #${currentSelectedIndex + 1}`;
  slideIndexBadge.textContent = `#${currentSelectedIndex + 1} of ${slides.length}`;

  slideTitle.value = slide.title || '';
  slideSubtitle.value = slide.subtitle || '';
  slideBadge.value = slide.badge || '';
  slideActionUrl.value = slide.actionUrl || '';
  slideImageUrl.value = slide.imageUrl || '';

  const sColor = slide.bgGradientStart || '#3d0f70';
  const eColor = slide.bgGradientEnd || '#d938a6';
  gradStart.value = sColor;
  gradStartHex.value = sColor;
  gradEnd.value = eColor;
  gradEndHex.value = eColor;

  if (slide.imageUrl) {
    setStyleTab('image');
    imagePreview.src = slide.imageUrl;
    imagePreviewContainer.classList.remove('hidden');
    dropzoneContent.classList.add('hidden');
  } else {
    setStyleTab('gradient');
    imagePreviewContainer.classList.add('hidden');
    dropzoneContent.classList.remove('hidden');
  }
}

function setStyleTab(tab) {
  if (tab === 'gradient') {
    tabGradient.classList.add('active');
    tabImage.classList.remove('active');
    sectionGradient.classList.remove('hidden');
    sectionImage.classList.add('hidden');
  } else {
    tabImage.classList.add('active');
    tabGradient.classList.remove('active');
    sectionImage.classList.remove('hidden');
    sectionGradient.classList.add('hidden');
  }
}

function updateCurrentSlideFromForm() {
  if (slides.length === 0) return;
  const slide = slides[currentSelectedIndex];

  slide.title = slideTitle.value;
  slide.subtitle = slideSubtitle.value;
  slide.badge = slideBadge.value;
  slide.actionUrl = slideActionUrl.value;
  slide.bgGradientStart = gradStart.value;
  slide.bgGradientEnd = gradEnd.value;
  slide.imageUrl = slideImageUrl.value;

  // Refresh preview and list thumbnail without full form reload
  renderSlideList();
  renderPreview();
}

function renderPreview() {
  if (slides.length === 0) return;
  if (previewIndex >= slides.length) previewIndex = 0;
  if (previewIndex < 0) previewIndex = slides.length - 1;

  const slide = slides[previewIndex];

  previewTitle.textContent = slide.title || 'UNTITLED SLIDE';
  previewSubtitle.textContent = slide.subtitle || '';
  previewBadge.textContent = slide.badge || 'NEWS';
  previewCounter.textContent = `Slide ${previewIndex + 1} of ${slides.length}`;

  // Background
  if (slide.imageUrl) {
    previewSlide.style.backgroundImage = `url('${slide.imageUrl}')`;
    previewSlide.style.background = `url('${slide.imageUrl}') center/cover no-repeat`;
  } else {
    const s = slide.bgGradientStart || '#3d0f70';
    const e = slide.bgGradientEnd || '#d938a6';
    previewSlide.style.background = `linear-gradient(135deg, ${s} 0%, ${e} 100%)`;
  }

  // Dots
  previewDots.innerHTML = '';
  slides.forEach((_, idx) => {
    const dot = document.createElement('div');
    dot.className = `preview-dot ${idx === previewIndex ? 'active' : ''}`;
    dot.addEventListener('click', (e) => {
      e.stopPropagation();
      previewIndex = idx;
      renderPreview();
    });
    previewDots.appendChild(dot);
  });
}

function stepPreview(delta) {
  previewIndex = (previewIndex + delta + slides.length) % slides.length;
  renderPreview();
}

// --- Add / Delete Slide ---
function handleAddSlide() {
  const newSlide = {
    id: `slide_${Date.now()}`,
    title: 'NEW ANNOUNCEMENT',
    subtitle: 'SPECIAL UPDATE • CHECK IT OUT',
    badge: 'NEW',
    bgGradientStart: '#144073',
    bgGradientEnd: '#26bfd9',
    imageUrl: '',
    actionUrl: ''
  };

  slides.push(newSlide);
  currentSelectedIndex = slides.length - 1;
  previewIndex = currentSelectedIndex;
  renderAll();
  showToast('Added new slide! Customize it in the editor.', 'success');
}

function handleDeleteCurrentSlide() {
  if (slides.length <= 1) {
    showToast('Carousel must have at least 1 slide.', 'error');
    return;
  }

  if (confirm(`Are you sure you want to delete slide "${slides[currentSelectedIndex].title}"?`)) {
    slides.splice(currentSelectedIndex, 1);
    if (currentSelectedIndex >= slides.length) {
      currentSelectedIndex = slides.length - 1;
    }
    previewIndex = currentSelectedIndex;
    renderAll();
    showToast('Slide deleted.', 'success');
  }
}

// --- Image Upload ---
function handleFileSelect(e) {
  const file = e.target.files[0];
  if (file) {
    uploadImageFile(file);
  }
}

async function uploadImageFile(file) {
  const formData = new FormData();
  formData.append('image', file);

  try {
    showToast('Uploading image banner...', 'success');
    const res = await fetch('/api/upload', {
      method: 'POST',
      body: formData
    });

    if (!res.ok) {
      const errData = await res.json();
      throw new Error(errData.error || 'Upload failed');
    }

    const data = await res.json();
    slideImageUrl.value = data.fullUrl || data.url;
    imagePreview.src = data.fullUrl || data.url;
    imagePreviewContainer.classList.remove('hidden');
    dropzoneContent.classList.add('hidden');

    updateCurrentSlideFromForm();
    showToast('Image uploaded successfully!', 'success');
  } catch (err) {
    console.error('Image upload failed:', err);
    showToast('Image upload failed: ' + err.message, 'error');
  }
}

// --- Helpers ---
function showToast(msg, type = 'info') {
  toastEl.textContent = msg;
  toastEl.className = `toast toast-${type}`;
  toastEl.classList.remove('hidden');

  setTimeout(() => {
    toastEl.classList.add('hidden');
  }, 3500);
}

function escapeHtml(text) {
  if (!text) return '';
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}
