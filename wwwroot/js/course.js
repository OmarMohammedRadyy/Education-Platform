// Theme Toggle Functionality
const themeToggle = document.getElementById('themeToggle');
const body = document.body;

function toggleTheme() {
    const isDark = body.classList.toggle('dark-mode');
    themeToggle.innerHTML = isDark ? '<i class="fas fa-sun"></i>' : '<i class="fas fa-moon"></i>';
    localStorage.setItem('theme', isDark ? 'dark' : 'light');
}

// Check saved theme preference
const savedTheme = localStorage.getItem('theme') || 'light';
if (savedTheme === 'dark') {
    body.classList.add('dark-mode');
    themeToggle.innerHTML = '<i class="fas fa-sun"></i>';
}
themeToggle.addEventListener('click', toggleTheme);

// Lesson Selection and Video Playback
let currentLessonId = null;
document.querySelectorAll('.lesson-card').forEach(card => {
    card.addEventListener('click', function () {
        // Update video
        const videoUrl = this.dataset.video;
        const videoContainer = document.querySelector('.video-container');

        // Add transition effect
        const currentIframe = videoContainer.querySelector('iframe');
        if (currentIframe) {
            currentIframe.style.opacity = '0';
            currentIframe.style.transform = 'scale(0.95)';

            setTimeout(() => {
                videoContainer.innerHTML = `<iframe src="${videoUrl}?autoplay=1" allowfullscreen style="opacity: 0; transform: scale(0.95); transition: all 0.3s ease;"></iframe>`;

                // Fade in the new video
                setTimeout(() => {
                    const newIframe = videoContainer.querySelector('iframe');
                    if (newIframe) {
                        newIframe.style.opacity = '1';
                        newIframe.style.transform = 'scale(1)';
                    }
                }, 50);
            }, 300);
        } else {
            videoContainer.innerHTML = `<iframe src="${videoUrl}?autoplay=1" allowfullscreen></iframe>`;
        }

        // Update active lesson state
        document.querySelectorAll('.lesson-card').forEach(c => c.classList.remove('active'));
        this.classList.add('active');

        // Update current lesson ID
        const lessonId = this.dataset.id;
        currentLessonId = lessonId;

        // Mark lesson as completed via API call
        fetch('/Payment/MarkLessonCompleted', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                lessonId: lessonId,
                courseId: document.querySelector('[data-course-id]')?.dataset.courseId
            })
        }).then(response => response.json()).then(data => {
            if (data.success) {
                // Update completion badge
                const badge = this.querySelector('.completed-badge');
                if (badge) {
                    badge.classList.remove('d-none');
                }

                // Show completion toast
                showToast('تم تسجيل التقدم', 'تم تسجيل مشاهدة الدرس بنجاح');

                // Handle certificate if available
                if (data.certificateUrl) {
                    showToast('تهانينا!', 'تم إصدار شهادتك بنجاح');
                    setTimeout(() => {
                        window.location.href = data.certificateUrl;
                    }, 3000);
                }
            }
        }).catch(error => {
            console.error('Error marking lesson as completed:', error);
        });

        // Show quiz button
        const quizBtn = document.getElementById('quizBtn');
        if (quizBtn) {
            quizBtn.style.display = 'flex';
        }
    });
});

// Quiz Button Functionality
function takeQuiz() {
    if (currentLessonId) {
        showToast('جاري التحميل', 'جاري تحويلك إلى صفحة الاختبار...');
        setTimeout(() => {
            window.location.href = `/Quiz/TakeQuiz?lessonId=${currentLessonId}`;
        }, 1000);
    }
}

// Notes Button Functionality
function openNotes() {
    const courseId = document.querySelector('[data-course-id]')?.dataset.courseId;
    showToast('جاري التحميل', 'جاري تحويلك إلى صفحة الملاحظات...');
    setTimeout(() => {
        window.location.href = `/Notes?courseId=${courseId}`;
    }, 1000);
}

// Toast Notification System
function showToast(title, message, duration = 3000) {
    // Create toast container if it doesn't exist
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.className = 'toast-container';
        document.body.appendChild(toastContainer);
    }

    // Create toast element
    const toast = document.createElement('div');
    toast.className = 'toast';

    toast.innerHTML = `
        <div class="toast-title">${title}</div>
        <div class="toast-message">${message}</div>
    `;

    toastContainer.appendChild(toast);

    // Remove toast after specified duration
    setTimeout(() => {
        toast.style.animation = 'toastOut 0.3s forwards';
        setTimeout(() => {
            toastContainer.removeChild(toast);
        }, 300);
    }, duration);
}

// Add course ID to container if not present in HTML
document.addEventListener('DOMContentLoaded', function () {
    // Extract course ID from the URL if needed and not provided in the HTML
    const urlParams = new URLSearchParams(window.location.search);
    const courseIdFromUrl = urlParams.get('courseId');

    if (courseIdFromUrl && !document.querySelector('[data-course-id]')) {
        document.querySelector('.course-container').dataset.courseId = courseIdFromUrl;
    }
});
