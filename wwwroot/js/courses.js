ocument.addEventListener('DOMContentLoaded', function () {
    // Helper function to add animation class when element is in viewport
    function handleScrollAnimation() {
        const cards = document.querySelectorAll('.course-card');

        const isInViewport = (element) => {
            const rect = element.getBoundingClientRect();
            return (
                rect.top <= (window.innerHeight || document.documentElement.clientHeight) &&
                rect.bottom >= 0
            );
        };

        const addAnimation = () => {
            cards.forEach(card => {
                if (isInViewport(card) && !card.classList.contains('is-visible')) {
                    card.classList.add('is-visible');
                }
            });
        };

        // Run on load
        addAnimation();

        // Run on scroll
        document.addEventListener('scroll', addAnimation);
    }

    // Initialize animations
    handleScrollAnimation();

    // Optional: Add hover effects for course cards
    const courseCards = document.querySelectorAll('.course-card');
    courseCards.forEach(card => {
        card.addEventListener('mouseenter', function () {
            this.querySelector('.image-overlay').style.opacity = '1';
        });

        card.addEventListener('mouseleave', function () {
            this.querySelector('.image-overlay').style.opacity = '0';
        });
    });
});
