// Seat Selection Module
(function () {
    'use strict';

    // Cache DOM elements
    const elements = {
        seatMap: document.getElementById('seat-map'),
        seatForm: document.getElementById('seatForm'),
        seatIdsContainer: document.getElementById('seatIdsContainer'),
        ticketTypeSection: document.getElementById('ticketTypeSection'),
        ticketWarning: document.getElementById('ticketWarning'),
        nextButton: document.getElementById('nextButton'),
        countDisplay: document.getElementById('count'),
        totalDisplay: document.getElementById('total'),
        totalSeatsDisplay: document.getElementById('totalSeatsSelected')
    };

    // State management
    const state = {
        ticketCounts: {
            children: 0,
            adult: 0,
            senior: 0
        },
        selectedSeatsCount: 0,
        prices: {
            ticket: parseFloat(elements.seatForm.dataset.ticketPrice),
            senior: parseFloat(elements.seatForm.dataset.seniorPrice)
        }
    };

    // Initialize the application
    function init() {
        attachEventListeners();
        updateUI();
    }

    // Attach all event listeners
    function attachEventListeners() {
        // Seat selection with event delegation
        elements.seatMap.addEventListener('click', handleSeatClick);

        // Ticket counter buttons with event delegation
        elements.ticketTypeSection.addEventListener('click', handleCounterClick);

        // Form submission
        elements.seatForm.addEventListener('submit', handleFormSubmit);
    }

    // Handle seat clicks
    function handleSeatClick(e) {
        const seat = e.target;

        if (!seat.classList.contains('seat') || seat.classList.contains('occupied')) {
            return;
        }

        seat.classList.toggle('selected');
        updateSelectedSeatsCount();
    }

    // Handle counter button clicks
    function handleCounterClick(e) {
        const button = e.target;

        if (!button.classList.contains('counter-btn')) {
            return;
        }

        const ticketCounter = button.closest('.ticket-counter');
        if (!ticketCounter) return;

        const ticketType = ticketCounter.dataset.ticketType;
        const action = button.dataset.action;
        const delta = action === 'increment' ? 1 : -1;

        changeTicketCount(ticketType, delta);
    }

    // Update selected seats count
    function updateSelectedSeatsCount() {
        const selectedSeats = elements.seatMap.querySelectorAll('.seat.selected');
        const previousCount = state.selectedSeatsCount;
        state.selectedSeatsCount = selectedSeats.length;

        // Auto-adjust adult count when seats are selected/deselected
        if (state.selectedSeatsCount > 0) {
            elements.ticketTypeSection.classList.add('show');

            const difference = state.selectedSeatsCount - previousCount;
            state.ticketCounts.adult = Math.max(0, state.ticketCounts.adult + difference);
        } else {
            elements.ticketTypeSection.classList.remove('show');
            resetTicketCounts();
        }

        updateUI();
    }

    // Change ticket count for a specific type
    function changeTicketCount(type, delta) {
        const totalTickets = getTotalTickets();
        const newTotal = totalTickets + delta;

        // Validate the change
        if (newTotal < 0 || newTotal > state.selectedSeatsCount) {
            return;
        }

        const newCount = state.ticketCounts[type] + delta;
        if (newCount >= 0) {
            state.ticketCounts[type] = newCount;
        }

        updateUI();
    }

    // Get total number of tickets
    function getTotalTickets() {
        return state.ticketCounts.children +
            state.ticketCounts.adult +
            state.ticketCounts.senior;
    }

    // Calculate total price
    function calculateTotal() {
        return (state.ticketCounts.children * state.prices.ticket) +
            (state.ticketCounts.adult * state.prices.ticket) +
            (state.ticketCounts.senior * state.prices.senior);
    }

    // Validate ticket counts match selected seats
    function validateTicketCounts() {
        const totalTickets = getTotalTickets();
        const isValid = totalTickets === state.selectedSeatsCount && state.selectedSeatsCount > 0;

        if (state.selectedSeatsCount > 0 && totalTickets !== state.selectedSeatsCount) {
            elements.ticketWarning.style.display = 'block';
            elements.nextButton.disabled = true;
        } else {
            elements.ticketWarning.style.display = 'none';
            elements.nextButton.disabled = state.selectedSeatsCount === 0;
        }

        return isValid;
    }

    // Reset all ticket counts
    function resetTicketCounts() {
        state.ticketCounts.children = 0;
        state.ticketCounts.adult = 0;
        state.ticketCounts.senior = 0;
    }

    // Update all UI elements
    function updateUI() {
        updateCountDisplays();
        updateTotalDisplay();
        updateSeatsDisplay();
        validateTicketCounts();
    }

    // Update count displays
    function updateCountDisplays() {
        // Update visual displays
        Object.keys(state.ticketCounts).forEach(type => {
            const display = document.querySelector(`[data-count-display="${type}"]`);
            if (display) {
                display.textContent = state.ticketCounts[type];
            }

            // Update hidden form inputs
            const input = document.querySelector(`[data-count-input="${type}"]`);
            if (input) {
                input.value = state.ticketCounts[type];
            }
        });
    }

    // Update total price display
    function updateTotalDisplay() {
        const total = calculateTotal();
        elements.totalDisplay.textContent = total.toFixed(2);
    }

    // Update selected seats display
    function updateSeatsDisplay() {
        elements.countDisplay.textContent = state.selectedSeatsCount;
        elements.totalSeatsDisplay.textContent = state.selectedSeatsCount;
    }

    // Handle form submission
    function handleFormSubmit(e) {
        const selectedSeats = elements.seatMap.querySelectorAll('.seat.selected');

        // Validate seat selection
        if (selectedSeats.length === 0) {
            e.preventDefault();
            alert('Please select at least one seat');
            return;
        }

        // Validate ticket counts
        const totalTickets = getTotalTickets();
        if (totalTickets !== selectedSeats.length) {
            e.preventDefault();
            alert('Ticket type count must match the number of selected seats');
            return;
        }

        // Add seat IDs to form
        elements.seatIdsContainer.innerHTML = '';
        selectedSeats.forEach(seat => {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'seatIds';
            input.value = seat.dataset.seatId;
            elements.seatIdsContainer.appendChild(input);
        });
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

})();