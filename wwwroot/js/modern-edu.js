// دالة التحميل
document.addEventListener('DOMContentLoaded', function () {
    setTimeout(() => {
        document.getElementById('loadingOverlay').style.opacity = '0';
        setTimeout(() => {
            document.getElementById('loadingOverlay').style.visibility = 'hidden';
        }, 500);
    }, 800);

    initializeApp();
});

function initializeApp() {
    setupSidebar();
    setupThemeToggle();
    setupLessons();
    setupModals();
    updateProgressBar();
}

// إعداد الشريط الجانبي
function setupSidebar() {
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebar = document.querySelector('.sidebar');

    if (sidebarToggle && sidebar) {
        sidebarToggle.addEventListener('click', () => {
            sidebar.classList.toggle('collapsed');
            localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('collapsed'));
        });

        // استرجاع حالة الشريط الجانبي من التخزين المحلي
        const sidebarCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
        if (sidebarCollapsed) {
            sidebar.classList.add('collapsed');
        }
    }
}

// إعداد وضع الظلام
function setupThemeToggle() {
    const themeToggle = document.getElementById('themeToggle');
    const body = document.body;

    if (themeToggle) {
        themeToggle.addEventListener('click', () => {
            body.classList.toggle('dark-mode');
            const isDark = body.classList.contains('dark-mode');
            themeToggle.innerHTML = isDark ? '<i class="fas fa-sun"></i>' : '<i class="fas fa-moon"></i>';
            localStorage.setItem('theme', isDark ? 'dark' : 'light');
        });

        // استرجاع الوضع من التخزين المحلي
        const savedTheme = localStorage.getItem('theme') || 'light';
        if (savedTheme === 'dark') {
            body.classList.add('dark-mode');
            themeToggle.innerHTML = '<i class="fas fa-sun"></i>';
        }
    }
}

// إعداد الدروس
function setupLessons() {
    const lessonCards = document.querySelectorAll('.lesson-card');
    const videoContainer = document.getElementById('videoContainer');
    const currentLessonTitle = document.querySelector('.current-lesson-title');
    const quizBtn = document.getElementById('quizBtn');

    // الحصول على معرّف الدورة من وسم البيانات
    const courseId = document.body.getAttribute('data-course-id');

    if (!lessonCards.length) return;

    // تهيئة حالة إكمال الدروس
    const completedLessons = JSON.parse(localStorage.getItem(`completedLessons-${courseId}`)) || [];

    lessonCards.forEach(card => {
        // تحديث البادج للدروس المكتملة
        const lessonId = card.getAttribute('data-id');
        if (completedLessons.includes(lessonId)) {
            card.querySelector('.completed-badge').classList.add('visible');
        }

        // إضافة معالج النقر
        card.addEventListener('click', function () {
            // إزالة الحالة النشطة من جميع البطاقات
            lessonCards.forEach(c => c.classList.remove('active'));

            // إضافة الحالة النشطة للبطاقة المحددة
            this.classList.add('active');

            // تحديث الفيديو
            const videoUrl = this.getAttribute('data-video');
            if (videoContainer) {
                videoContainer.innerHTML = `<iframe src="${videoUrl}?autoplay=1" allowfullscreen loading="lazy"></iframe>`;
            }

            // تحديث عنوان الدرس الحالي
            if (currentLessonTitle) {
                currentLessonTitle.textContent = this.querySelector('.lesson-title').textContent;
            }

            // تحديث حالة إكمال الدرس
            const lessonId = this.getAttribute('data-id');
            if (!completedLessons.includes(lessonId)) {
                completedLessons.push(lessonId);
                localStorage.setItem(`completedLessons-${courseId}`, JSON.stringify(completedLessons));
                this.querySelector('.completed-badge').classList.add('visible');
            }

            // إظهار زر الاختبار
            if (quizBtn) {
                quizBtn.style.display = 'flex';
                quizBtn.onclick = function () {
                    window.location.href = `/Quiz/TakeQuiz?lessonId=${lessonId}`;
                };
            }

            // تحديث شريط التقدم
            updateProgressBar();
        });
    });

    // تحديد الدرس النشط الافتراضي (الأول)
    if (lessonCards.length > 0 && !document.querySelector('.lesson-card.active')) {
        lessonCards[0].classList.add('active');
    }
}

// تحديث شريط التقدم
function updateProgressBar() {
    const progressFilled = document.getElementById('progressFilled');
    const progressPercent = document.getElementById('progressPercent');
    const lessonCards = document.querySelectorAll('.lesson-card');

    if (!progressFilled || !progressPercent || !lessonCards.length) return;

    const courseId = document.body.getAttribute('data-course-id');
    const completedLessons = JSON.parse(localStorage.getItem(`completedLessons-${courseId}`)) || [];

    const totalLessons = lessonCards.length;
    const completedCount = completedLessons.length;
    const percentComplete = Math.round((completedCount / totalLessons) * 100);

    progressFilled.style.width = `${percentComplete}%`;
    progressPercent.textContent = `${percentComplete}%`;
}

// إعداد النوافذ المنبثقة
function setupModals() {
    // نافذة الملاحظات
    const notesBtn = document.querySelector('.notes-button');
    const notesModal = document.getElementById('notesModal');
    const closeNotesModal = document.getElementById('closeNotesModal');
    const notesArea = document.getElementById('notesArea');
    const saveNotesBtn = document.getElementById('saveNotes');

    if (notesBtn && notesModal) {
        notesBtn.addEventListener('click', () => {
            notesModal.classList.add('open');

            // استرجاع الملاحظات المحفوظة
            const courseId = document.body.getAttribute('data-course-id');
            const savedNotes = localStorage.getItem(`notes-${courseId}`) || '';
            if (notesArea) {
                notesArea.value = savedNotes;
            }
        });
    }

    if (closeNotesModal && notesModal) {
        closeNotesModal.addEventListener('click', () => {
            notesModal.classList.remove('open');
        });
    }

    if (saveNotesBtn && notesArea) {
        saveNotesBtn.addEventListener('click', () => {
            const courseId = document.body.getAttribute('data-course-id');
            localStorage.setItem(`notes-${courseId}`, notesArea.value);

            // إظهار رسالة نجاح (يمكن استخدام مكتبة toast)
            alert('تم حفظ الملاحظات بنجاح');

            // إغلاق النافذة
            if (notesModal) {
                notesModal.classList.remove('open');
            }
        });
    }

    // نافذة الموارد
    const resourcesBtn = document.getElementById('resourcesBtn');
    const resourcesModal = document.getElementById('resourcesModal');
    const closeResourcesModal = document.getElementById('closeResourcesModal');

    if (resourcesBtn && resourcesModal) {
        resourcesBtn.addEventListener('click', () => {
            resourcesModal.classList.add('open');

            // هنا يمكنك تحميل الموارد المرتبطة بالدرس الحالي من الخادم
            // للعرض التوضيحي، سنستخدم المحتوى الافتراضي المحدد في HTML
        });
    }

    if (closeResourcesModal && resourcesModal) {
        closeResourcesModal.addEventListener('click', () => {
            resourcesModal.classList.remove('open');
        });
    }

    // إغلاق النوافذ المنبثقة عند النقر خارجها
    window.addEventListener('click', (e) => {
        if (e.target === notesModal) {
            notesModal.classList.remove('open');
        }
        if (e.target === resourcesModal) {
            resourcesModal.classList.remove('open');
        }
    });
}

// دالة فتح الملاحظات
function openNotes() {
    const notesModal = document.getElementById('notesModal');
    if (notesModal) {
        notesModal.classList.add('open');

        // استرجاع الملاحظات المحفوظة
        const courseId = document.body.getAttribute('data-course-id');
        const savedNotes = localStorage.getItem(`notes-${courseId}`) || '';
        const notesArea = document.getElementById('notesArea');
        if (notesArea) {
            notesArea.value = savedNotes;
        }
    }
}

// استدعاء دالة التهيئة عند تحميل المستند
document.addEventListener('DOMContentLoaded', initializeApp);