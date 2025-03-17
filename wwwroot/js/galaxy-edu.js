// تحويل رابط YouTube إلى صيغة Embed
function getEmbedUrl(url) {
    if (!url) return "";

    if (url.includes("youtu.be")) {
        const videoId = url.split('/').pop().split('?')[0];
        return `https://www.youtube.com/embed/${videoId}`;
    }

    if (url.includes("watch?v=")) {
        const videoId = url.split('=')[1].split('&')[0];
        return `https://www.youtube.com/embed/${videoId}`;
    }

    return url;
}

// المتغيرات العامة
let currentLessonIndex = 0;
let isDarkMode = false;
let isSidebarCollapsed = false;
let notes = {};

// بدء التطبيق عند تحميل الصفحة
document.addEventListener('DOMContentLoaded', () => {
    setupEventListeners();
    initializeLessons();

    // إخفاء شاشة التحميل بعد فترة
    setTimeout(() => {
        const loadingOverlay = document.getElementById('loadingOverlay');
        loadingOverlay.style.opacity = '0';
        loadingOverlay.style.pointerEvents = 'none';
    }, 1500);
});

// إعداد المستمعين للأحداث
function setupEventListeners() {
    // تبديل وضع الشريط الجانبي
    const sidebarToggle = document.getElementById('sidebarToggle');
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', toggleSidebar);
    }

    // تبديل السمة (الوضع المظلم/الفاتح)
    const themeToggle = document.getElementById('themeToggle');
    if (themeToggle) {
        themeToggle.addEventListener('click', toggleDarkMode);
    }

    // فتح مودال الملاحظات
    const notesBtn = document.getElementById('notesBtn');
    if (notesBtn) {
        notesBtn.addEventListener('click', openNotes);
    }

    // إغلاق مودال الملاحظات
    const closeNotesModal = document.getElementById('closeNotesModal');
    if (closeNotesModal) {
        closeNotesModal.addEventListener('click', closeNotes);
    }

    // حفظ الملاحظات
    const saveNotes = document.getElementById('saveNotes');
    if (saveNotes) {
        saveNotes.addEventListener('click', saveUserNotes);
    }

    // فتح مودال الموارد
    const resourcesBtn = document.getElementById('resourcesBtn');
    if (resourcesBtn) {
        resourcesBtn.addEventListener('click', openResources);
    }

    // إغلاق مودال الموارد
    const closeResourcesModal = document.getElementById('closeResourcesModal');
    if (closeResourcesModal) {
        closeResourcesModal.addEventListener('click', closeResources);
    }

    // إضافة مستمع للنقر خارج المودال لإغلاقه
    window.addEventListener('click', (event) => {
        const notesModal = document.getElementById('notesModal');
        const resourcesModal = document.getElementById('resourcesModal');

        if (event.target === notesModal) {
            closeNotes();
        }
        if (event.target === resourcesModal) {
            closeResources();
        }
    });
}

// تهيئة الدروس
function initializeLessons() {
    const lessonCards = document.querySelectorAll('.lesson-card');

    if (lessonCards.length > 0) {
        // إضافة مستمعي الأحداث للدروس
        lessonCards.forEach((card, index) => {
            card.addEventListener('click', () => {
                const lessonId = card.getAttribute('data-id');
                const videoUrl = card.getAttribute('data-video');

                if (index !== currentLessonIndex) {
                    changeLesson(index, lessonId, videoUrl);
                }
            });
        });

        // تحديد الدرس الأول كنشط افتراضيًا
        lessonCards[0].classList.add('active');

        // تحديث مؤشر التقدم
        updateProgress(1, lessonCards.length);
    }
}

// تبديل وضع الشريط الجانبي
function toggleSidebar() {
    const appContainer = document.getElementById('appContainer');
    const sidebar = document.querySelector('.sidebar');

    if (window.innerWidth <= 768) {
        // على الشاشات الصغيرة
        sidebar.classList.toggle('open');
    } else {
        // على الشاشات الكبيرة
        isSidebarCollapsed = !isSidebarCollapsed;
        appContainer.classList.toggle('sidebar-collapsed');

        const sidebarToggle = document.getElementById('sidebarToggle');
        if (isSidebarCollapsed) {
            sidebarToggle.innerHTML = '<i class="fas fa-arrow-left"></i>';
        } else {
            sidebarToggle.innerHTML = '<i class="fas fa-arrow-right"></i>';
        }
    }
}

// تبديل الوضع المظلم/الفاتح
function toggleDarkMode() {
    isDarkMode = !isDarkMode;
    document.documentElement.classList.toggle('dark');

    const themeToggle = document.getElementById('themeToggle');
    if (isDarkMode) {
        themeToggle.innerHTML = '<i class="fas fa-sun"></i>';
    } else {
        themeToggle.innerHTML = '<i class="fas fa-moon"></i>';
    }
}

// تغيير الدرس الحالي
function changeLesson(index, lessonId, videoUrl) {
    currentLessonIndex = index;

    // إزالة الفئة النشطة من جميع الدروس
    const lessonCards = document.querySelectorAll('.lesson-card');
    lessonCards.forEach(card => card.classList.remove('active'));

    // إضافة الفئة النشطة للدرس المحدد
    lessonCards[index].classList.add('active');

    // تحديث الفيديو
    const videoContainer = document.getElementById('videoContainer');
    if (videoContainer && videoUrl) {
        videoContainer.innerHTML = `
      <iframe src="${videoUrl}" allowfullscreen loading="lazy"></iframe>
    `;
    }

    // تحديث معلومات الدرس
    const lessonTitle = lessonCards[index].querySelector('.lesson-title').textContent;
    const lessonDescription = lessonCards[index].getAttribute('data-description') || '';

    const currentLessonInfo = document.getElementById('currentLessonInfo');
    if (currentLessonInfo) {
        currentLessonInfo.innerHTML = `
      <h2 class="current-lesson-title">${lessonTitle}</h2>
      <div class="lesson-description">
        <p>${lessonDescription}</p>
      </div>
    `;
    }

    // تحديث ملفات الدرس
    updateResources(lessonId);

    // تحديث مؤشر التقدم
    updateProgress(index + 1, lessonCards.length);

    // إظهار زر الاختبار (مثال: نظهره للدروس المكتملة)
    const quizBtn = document.getElementById('quizBtn');
    if (quizBtn) {
        const isCompleted = lessonCards[index].querySelector('.completed-badge') !== null;
        quizBtn.style.display = isCompleted ? 'flex' : 'none';


    }

    

}

// تحديث مؤشر التقدم
function updateProgress(current, total) {
    const progressPercent = Math.round((current / total) * 100);
    const progressPercentEl = document.getElementById('progressPercent');
    const progressFilled = document.getElementById('progressFilled');

    if (progressPercentEl) {
        progressPercentEl.textContent = `${progressPercent}%`;
    }

    if (progressFilled) {
        progressFilled.style.width = `${progressPercent}%`;
    }
}

// تحديث قائمة الملفات
function updateResources(lessonId) {
    const resourcesList = document.getElementById('resourcesList');
    if (!resourcesList) return;

    // في التطبيق الفعلي، ستقوم بالتحقق من وجود ملفات مرتبطة بالدرس
    // هنا سنستخدم مثال بسيط

    // التحقق من وجود ملفات للدرس (يمكن تغييره باستخدام البيانات الفعلية)
    const hasResources = lessonId && lessonId !== "0";

    if (hasResources) {
        resourcesList.innerHTML = `
      <div class="resource-item">
        <div class="resource-icon"><i class="fas fa-file-pdf"></i></div>
        <div class="resource-info">
          <h4>ملخص الدرس</h4>
          <p>ملف PDF يحتوي على ملخص للدرس</p>
        </div>
        <button class="download-button"><i class="fas fa-download"></i></button>
      </div>
      <div class="resource-item">
        <div class="resource-icon"><i class="fas fa-file-code"></i></div>
        <div class="resource-info">
          <h4>أمثلة برمجية</h4>
          <p>ملفات تحتوي على أمثلة برمجية للدرس</p>
        </div>
        <button class="download-button"><i class="fas fa-download"></i></button>
      </div>
    `;
    } else {
        resourcesList.innerHTML = `
      <div class="empty-resources">
        <div class="empty-icon"><i class="fas fa-folder-open"></i></div>
        <p>لا توجد ملفات متاحة لهذا الدرس</p>
      </div>
    `;
    }
}

// فتح نافذة الملاحظات
function openNotes() {
    const notesModal = document.getElementById('notesModal');
    if (!notesModal) return;

    notesModal.classList.add('active');

    const currentLessonId = document.querySelectorAll('.lesson-card')[currentLessonIndex]?.getAttribute('data-id');
    const notesArea = document.getElementById('notesArea');

    if (notesArea && currentLessonId) {
        // استرجاع ملاحظات الدرس الحالي إن وجدت
        notesArea.value = notes[currentLessonId] || '';
        notesArea.focus();
    }
}

// إغلاق نافذة الملاحظات
function closeNotes() {
    const notesModal = document.getElementById('notesModal');
    if (notesModal) {
        notesModal.classList.remove('active');
    }
}

// حفظ ملاحظات المستخدم
function saveUserNotes() {
    const notesArea = document.getElementById('notesArea');
    const currentLessonId = document.querySelectorAll('.lesson-card')[currentLessonIndex]?.getAttribute('data-id');

    if (notesArea && currentLessonId) {
        // حفظ ملاحظات الدرس الحالي
        notes[currentLessonId] = notesArea.value;

        // يمكن حفظها في localStorage ليتم استرجاعها لاحقًا
        try {
            localStorage.setItem('course_notes', JSON.stringify(notes));
        } catch (e) {
            console.error('خطأ في حفظ الملاحظات:', e);
        }

        // إظهار رسالة نجاح الحفظ (يمكن استخدام مكتبة toast)
        showToast('تم حفظ ملاحظاتك بنجاح');
    }

    closeNotes();
}

// إظهار رسالة toast
function showToast(message) {
    // مثال بسيط لرسالة toast
    const toast = document.createElement('div');
    toast.className = 'toast';
    toast.textContent = message;

    document.body.appendChild(toast);

    setTimeout(() => {
        toast.classList.add('show');
    }, 100);

    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => {
            document.body.removeChild(toast);
        }, 300);
    }, 3000);
}

// فتح نافذة الملفات
function openResources() {
    const resourcesModal = document.getElementById('resourcesModal');
    if (resourcesModal) {
        resourcesModal.classList.add('active');
    }
}

// إغلاق نافذة الملفات
function closeResources() {
    const resourcesModal = document.getElementById('resourcesModal');
    if (resourcesModal) {
        resourcesModal.classList.remove('active');
    }
}

// استرجاع الملاحظات المحفوظة عند تحميل الصفحة
function loadSavedNotes() {
    try {
        const savedNotes = localStorage.getItem('course_notes');
        if (savedNotes) {
            notes = JSON.parse(savedNotes);
        }
    } catch (e) {
        console.error('خطأ في استرجاع الملاحظات:', e);
        notes = {};
    }
}

// استدعاء وظيفة تحميل الملاحظات
loadSavedNotes();

// إضافة نمط CSS للرسائل Toast
document.head.insertAdjacentHTML('beforeend', `
  <style>
    .toast {
      position: fixed;
      bottom: 20px;
      left: 50%;
      transform: translateX(-50%) translateY(100px);
      background-color: var(--success-color);
      color: white;
      padding: 12px 24px;
      border-radius: 8px;
      font-weight: 500;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      z-index: 1000;
      opacity: 0;
      transition: transform 0.3s, opacity 0.3s;
    }
    
    .toast.show {
      transform: translateX(-50%) translateY(0);
      opacity: 1;
    }
  </style>
`);
