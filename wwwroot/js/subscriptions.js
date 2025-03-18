// Wait for the DOM to be fully loaded
document.addEventListener('DOMContentLoaded', () => {
    // Element references
    const themeToggle = document.getElementById('themeToggle');
    const tableRows = document.querySelectorAll('.subscription-row');
    const tableCells = document.querySelectorAll('.subscription-table tbody td');

    // Set up responsive table for mobile
    setupResponsiveTable();

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

    // Add animation to table rows
    tableRows.forEach((row, index) => {
        row.style.animationDelay = `${index * 100}ms`;
        row.classList.add('fade-in');
    });

    // Function to set up responsive table for mobile
    function setupResponsiveTable() {
        if (window.innerWidth <= 768) {
            const tableHeaders = document.querySelectorAll('.subscription-table thead th');
            const headerTexts = Array.from(tableHeaders).map(header => header.textContent);

            tableCells.forEach((cell, index) => {
                const headerIndex = index % headerTexts.length;
                cell.setAttribute('data-label', headerTexts[headerIndex]);
            });
        }
    }

    // Listen for window resize events
    window.addEventListener('resize', setupResponsiveTable);

    // Parse subscription data from Razor view if available
    try {
        // This will be populated by the Razor view engine
        const subscriptions = subscriptionsData || [];
        console.log('Loaded subscriptions:', subscriptions.length);

        // Additional client-side processing can be done here
        // For example, sorting subscriptions by completion percentage

        // Calculate average completion
        if (subscriptions.length > 0) {
            const totalCompletion = subscriptions.reduce((sum, sub) => sum + sub.completionPercentage, 0);
            const averageCompletion = totalCompletion / subscriptions.length;
            console.log(`Average completion: ${averageCompletion.toFixed(2)}%`);
        }
    } catch (error) {
        // Subscriptions data may not be available in preview mode
        console.log('Subscriptions data not available:', error);
    }

    // Handle button clicks with animation
    const buttons = document.querySelectorAll('.btn');
    buttons.forEach(button => {
        button.addEventListener('click', function (e) {
            // Add ripple effect
            const ripple = document.createElement('span');
            ripple.classList.add('ripple');
            this.appendChild(ripple);

            const diameter = Math.max(this.clientWidth, this.clientHeight);
            ripple.style.width = ripple.style.height = `${diameter}px`;

            const rect = this.getBoundingClientRect();
            ripple.style.left = `${e.clientX - rect.left - diameter / 2}px`;
            ripple.style.top = `${e.clientY - rect.top - diameter / 2}px`;

            setTimeout(() => {
                ripple.remove();
            }, 600);
        });
    });
});
