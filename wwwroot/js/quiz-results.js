// Wait for the DOM to be fully loaded
document.addEventListener('DOMContentLoaded', () => {
    // Element references
    const themeToggle = document.getElementById('themeToggle');
    const shareBtn = document.getElementById('shareBtn');
    const shareModal = document.getElementById('shareModal');
    const closeModal = document.getElementById('closeModal');
    const copyLinkBtn = document.getElementById('copyLinkBtn');
    const shareLinkInput = document.getElementById('shareLink');

    // Check for saved theme preference
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme === 'dark') {
        document.body.classList.add('dark-theme');
        themeToggle.innerHTML = '<i class="fas fa-sun"></i>';
    }

    // Theme toggle functionality
    themeToggle.addEventListener('click', () => {
        document.body.classList.toggle('dark-theme');

        if (document.body.classList.contains('dark-theme')) {
            localStorage.setItem('theme', 'dark');
            themeToggle.innerHTML = '<i class="fas fa-sun"></i>';
        } else {
            localStorage.setItem('theme', 'light');
            themeToggle.innerHTML = '<i class="fas fa-moon"></i>';
        }
    });

    // Share modal functionality
    if (shareBtn && shareModal && closeModal) {
        // Open modal
        shareBtn.addEventListener('click', () => {
            shareModal.classList.add('show');
        });

        // Close modal
        closeModal.addEventListener('click', () => {
            shareModal.classList.remove('show');
        });

        // Close modal when clicking outside
        shareModal.addEventListener('click', (e) => {
            if (e.target === shareModal) {
                shareModal.classList.remove('show');
            }
        });

        // Copy link functionality
        if (copyLinkBtn && shareLinkInput) {
            copyLinkBtn.addEventListener('click', () => {
                shareLinkInput.select();
                document.execCommand('copy');

                // Visual feedback for copy
                const originalText = copyLinkBtn.innerHTML;
                copyLinkBtn.innerHTML = '<i class="fas fa-check"></i>';
                copyLinkBtn.style.backgroundColor = 'var(--success)';

                setTimeout(() => {
                    copyLinkBtn.innerHTML = originalText;
                    copyLinkBtn.style.backgroundColor = '';
                }, 2000);
            });
        }

        // Share options functionality
        const shareOptions = document.querySelectorAll('.share-option');
        if (shareOptions.length) {
            shareOptions.forEach(option => {
                option.addEventListener('click', () => {
                    const url = shareLinkInput.value;
                    const text = 'لقد أكملت اختباري! شاهد النتيجة:';

                    let shareUrl = '';

                    if (option.classList.contains('whatsapp')) {
                        shareUrl = `https://wa.me/?text=${encodeURIComponent(text + ' ' + url)}`;
                    } else if (option.classList.contains('telegram')) {
                        shareUrl = `https://t.me/share/url?url=${encodeURIComponent(url)}&text=${encodeURIComponent(text)}`;
                    } else if (option.classList.contains('twitter')) {
                        shareUrl = `https://twitter.com/intent/tweet?text=${encodeURIComponent(text)}&url=${encodeURIComponent(url)}`;
                    } else if (option.classList.contains('facebook')) {
                        shareUrl = `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(url)}`;
                    }

                    if (shareUrl) {
                        window.open(shareUrl, '_blank');
                    }
                });
            });
        }
    }

    // Add animations to elements as they come into view
    const animateOnScroll = () => {
        const elements = document.querySelectorAll('.answer-card');

        elements.forEach(element => {
            const elementPosition = element.getBoundingClientRect().top;
            const screenPosition = window.innerHeight / 1.2;

            if (elementPosition < screenPosition) {
                element.style.opacity = '1';
                element.style.transform = 'translateY(0)';
            }
        });
    };

    // Initial call and scroll event listener
    animateOnScroll();
    window.addEventListener('scroll', animateOnScroll);

    // Function to generate charts for score visualization
    const generateCharts = () => {
        // This is a placeholder for potential chart functionality
        // Using a library like Chart.js could be implemented here
        console.log('Charts functionality could be added here');
    };

    // Optional: Call chart generation if needed
    // generateCharts();
});