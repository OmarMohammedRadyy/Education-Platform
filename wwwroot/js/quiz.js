document.addEventListener('DOMContentLoaded', function () {
    let currentQuestionIndex = 0;
    const questionCards = document.querySelectorAll('.question-card');
    const prevBtn = document.getElementById('prevBtn');
    const nextBtn = document.getElementById('nextBtn');
    const submitBtn = document.getElementById('submitBtn');
    const progressFill = document.getElementById('progressFill');
    const progressText = document.getElementById('progressText');
    const timerDisplay = document.getElementById('timerDisplay');
    const themeToggle = document.getElementById('themeToggle');
    const quizForm = document.getElementById('quizForm');

    let startTime = new Date();
    let timerInterval;

    // Initialize the timer
    function startTimer() {
        timerInterval = setInterval(updateTimer, 1000);
    }

    // Update the timer display
    function updateTimer() {
        const currentTime = new Date();
        const elapsedTime = Math.floor((currentTime - startTime) / 1000);
        const minutes = Math.floor(elapsedTime / 60);
        const seconds = elapsedTime % 60;
        timerDisplay.textContent = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
    }

    // Show the current question and hide others
    function showQuestion(index) {
        questionCards.forEach((card, i) => {
            if (i === index) {
                card.classList.add('active');
            } else {
                card.classList.remove('active');
            }
        });

        // Update navigation buttons
        prevBtn.disabled = index === 0;
        if (index === questionCards.length - 1) {
            nextBtn.style.display = 'none';
            submitBtn.style.display = 'inline-flex';
        } else {
            nextBtn.style.display = 'inline-flex';
            submitBtn.style.display = 'none';
        }

        // Update progress
        const progress = ((index + 1) / questionCards.length) * 100;
        progressFill.style.width = `${progress}%`;
        progressText.textContent = `${Math.round(progress)}%`;
    }

    // Navigation event handlers
    prevBtn.addEventListener('click', () => {
        if (currentQuestionIndex > 0) {
            currentQuestionIndex--;
            showQuestion(currentQuestionIndex);
        }
    });

    nextBtn.addEventListener('click', () => {
        const currentQuestion = questionCards[currentQuestionIndex];
        const inputs = currentQuestion.querySelectorAll('input, textarea');
        let isValid = true;

        // Check if the current question has been answered
        if (currentQuestion.querySelector('input[type="radio"]')) {
            const radios = currentQuestion.querySelectorAll('input[type="radio"]:checked');
            if (radios.length === 0) {
                isValid = false;
                showError(currentQuestion, 'الرجاء اختيار إجابة');
            }
        } else if (currentQuestion.querySelector('textarea')) {
            const textarea = currentQuestion.querySelector('textarea');
            if (!textarea.value.trim()) {
                isValid = false;
                showError(currentQuestion, 'الرجاء كتابة إجابتك');
            }
        }

        if (isValid && currentQuestionIndex < questionCards.length - 1) {
            currentQuestionIndex++;
            showQuestion(currentQuestionIndex);
            clearErrors();
        }
    });

    // Form submission handler
    quizForm.addEventListener('submit', function (e) {
        const allQuestions = document.querySelectorAll('.question-card');
        let allAnswered = true;

        // Check if all questions are answered
        allQuestions.forEach((question, index) => {
            if (question.querySelector('input[type="radio"]')) {
                const radios = question.querySelectorAll('input[type="radio"]:checked');
                if (radios.length === 0) {
                    allAnswered = false;
                    currentQuestionIndex = index;
                    showQuestion(index);
                    showError(question, 'الرجاء اختيار إجابة');
                }
            } else if (question.querySelector('textarea')) {
                const textarea = question.querySelector('textarea');
                if (!textarea.value.trim()) {
                    allAnswered = false;
                    currentQuestionIndex = index;
                    showQuestion(index);
                    showError(question, 'الرجاء كتابة إجابتك');
                }
            }
        });

        if (!allAnswered) {
            e.preventDefault();
        }
    });

    // Show error message
    function showError(questionElement, message) {
        clearErrors();

        const errorDiv = document.createElement('div');
        errorDiv.className = 'feedback-message error';
        errorDiv.innerHTML = `<i class="fas fa-exclamation-circle"></i> ${message}`;

        const footer = questionElement.querySelector('.question-footer');
        questionElement.insertBefore(errorDiv, footer);
    }

    // Clear all error messages
    function clearErrors() {
        const errors = document.querySelectorAll('.feedback-message');
        errors.forEach(error => error.remove());
    }

    // Theme toggle functionality
    themeToggle.addEventListener('click', () => {
        document.body.classList.toggle('dark-mode');
        const icon = themeToggle.querySelector('i');

        if (document.body.classList.contains('dark-mode')) {
            icon.classList.remove('fa-moon');
            icon.classList.add('fa-sun');
        } else {
            icon.classList.remove('fa-sun');
            icon.classList.add('fa-moon');
        }
    });

    // Initialize the first question and start the timer
    showQuestion(currentQuestionIndex);
    startTimer();
});
