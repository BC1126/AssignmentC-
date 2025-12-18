// Seat Selection with Real-time Locking - FIXED VERSION
(function () {
    'use strict';
    let connection;
    const elements = {
        seatMap: document.getElementById('seat-map'),
        seatForm: document.getElementById('seatForm'),
        nextButton: document.getElementById('nextButton'),
        ticketWarning: document.getElementById('ticketWarning'),
        countDisplay: document.getElementById('count'),
        totalDisplay: document.getElementById('total'),
        totalSeatsDisplay: document.getElementById('totalSeatsSelected'),
        ticketTypeSection: document.getElementById('ticketTypeSection'),
        seatTimer: document.getElementById('seatTimer'),
        timerDisplay: document.getElementById('timerDisplay'),
        childrenInput: null,
        adultInput: null,
        seniorInput: null,
        okuInput: document.querySelector('input[name="OkuCount"]'),
    };

    const state = {
        showTimeId: 0,
        selectedSeats: new Set(),
        clientSessionId: Math.random().toString(36).substring(2, 15),
        isLocking: false
    };

    function init() {
        if (!elements.seatMap || !elements.seatForm) {
            console.error('Required elements not found');
            return;
        }

        // Get configuration
        state.showTimeId = parseInt(elements.seatForm.dataset.showtimeId) || 0;
        state.lockDurationMinutes = parseInt(elements.seatForm.dataset.lockDuration) || 5;

        // Cache input elements
        elements.childrenInput = document.querySelector('input[name="ChildrenCount"]');
        elements.adultInput = document.querySelector('input[name="AdultCount"]');
        elements.seniorInput = document.querySelector('input[name="SeniorCount"]');

        if (!elements.childrenInput || !elements.adultInput || !elements.seniorInput) {
            console.error('Ticket count inputs not found');
            return;
        }
        document.querySelectorAll('.seat.selected').forEach(seat => {
            const seatId = parseInt(seat.dataset.seatId);
            state.selectedSeats.add(seatId);
        });

        // If there are seats selected, update the count and pricing UI
        if (state.selectedSeats.size > 0) {
            updateSeatCount();
        }
        // Event listeners
        elements.seatMap.addEventListener('click', handleSeatClick);
        document.querySelectorAll('.counter-btn').forEach(btn => {
            btn.addEventListener('click', handleCounterChange);
        });
        elements.seatForm.addEventListener('submit', handleFormSubmit);

        // Handle page unload - release locks
        window.addEventListener('beforeunload', () => {
            if (state.selectedSeats.size > 0) {
                releaseSeatsSync();
            }
        });

        // Initial state
        if (elements.nextButton) {
            elements.nextButton.disabled = true;
        }

        console.log('Seat locking system initialized');
        if (typeof signalR !== 'undefined') {
            setupSignalR();
        } else {
            console.error("SignalR library not found! Real-time updates disabled.");
        }       
    }
    function setupSignalR() {
        connection = new signalR.HubConnectionBuilder()
            .withUrl("/seathub")
            .withAutomaticReconnect()
            .build();

            connection.on("SeatStatusChanged", (showTimeId, seatIds, status) => {
                console.log("📢 SignalR Update Received:", { showTimeId, seatIds, status });

                if (Number(showTimeId) !== Number(state.showTimeId)) {
                    console.log("❌ Update ignored: ShowTimeId mismatch");
                    return;
                }

                seatIds.forEach(id => {
                    const seatId = Number(id);
                    const seatEl = document.querySelector(`[data-seat-id="${seatId}"]`);

                    if (!seatEl) return;

                    // CRITICAL: If I have this seat selected, don't let SignalR turn it grey!
                    if (state.selectedSeats.has(seatId)) {
                        console.log(`Ignoring update for seat ${seatId} because I own it.`);
                        return;
                    }

                    if (status === "locked") {
                        seatEl.classList.add("locked");
                        console.log(`Seat ${seatId} is now LOCKED for others`);
                    } else if (status === "available") {
                        seatEl.classList.remove("locked");
                        console.log(`Seat ${seatId} is now AVAILABLE`);
                    }
                });
        });

        connection.start()
            .then(() => console.log("✅ SignalR Connected"))
            .catch(err => console.error("❌ SignalR Error: ", err));
    }
    window.addEventListener('beforeunload', () => {
        if (state.selectedSeats.size > 0) {
            const data = {
                showTimeId: state.showTimeId,
                seatIds: Array.from(state.selectedSeats)
            };
            navigator.sendBeacon('/Booking/ReleaseSeats', JSON.stringify(data));
        }
    });
    async function handleSeatClick(e) {
        const seatEl = e.target;
        if (!seatEl.classList.contains('seat') || seatEl.classList.contains('occupied') || seatEl.classList.contains('locked')) {
            return;
        }

        const seatId = parseInt(seatEl.dataset.seatId);

        if (state.selectedSeats.has(seatId)) {
            const released = await releaseSingleSeat(seatId);
            if (released) {
                state.selectedSeats.delete(seatId);
                seatEl.classList.remove('selected');
                updateSeatCount();
                console.log(`Seat ${seatId} released.`);
            }
            return; 
        }

        if (seatEl.classList.contains('wheelchair')) {
            const okuInput = document.querySelector('input[name="OkuCount"]');
            const okuCount = parseInt(okuInput?.value) || 0;

            if (okuCount === 0) {
                alert("Wheelchair seats are reserved for OKU ticket holders. Please add an OKU ticket type first.");
                return; 
            }
        }

        const locked = await lockSingleSeat(seatId);
        if (locked) {
            state.selectedSeats.add(seatId);
            seatEl.classList.add('selected');
            updateSeatCount();
        }
    }



    async function lockSingleSeat(seatId) {
        try {
            const res = await fetch('/Booking/LockSeats', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    showTimeId: state.showTimeId,
                    seatIds: [seatId]
                })
            });

            const data = await res.json();

            if (!data.success) {
                throw new Error(data.message || 'Seat is held');
            }

            // START THE TIMER HERE
            if (data.expiresAt) {
                state.lockExpiresAt = new Date(data.expiresAt);
                startTimer();
            }

            return true;
        } catch (err) {
            console.error(err);

            const seatEl = document.querySelector(`[data-seat-id="${seatId}"]`);
            seatEl?.classList.remove('selected');
            state.selectedSeats.delete(seatId);
            updateSeatCount();

            alert(err.message);
            return false;
        }
    }



    async function releaseSingleSeat(seatId) {
        try {
            const res = await fetch('/Booking/ReleaseSeats', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    showTimeId: state.showTimeId,
                    seatIds: [seatId]
                })
            });

            const data = await res.json();
            if (data.success) {
                console.log(`Seat ${seatId} released in DB and broadcasted.`);
                return true;
            }
        } catch (err) {
            console.error("Failed to release seat:", err);
        }
        return false;
    }


    function handleCounterChange(e) {
        const btn = e.target;
        if (!btn.classList.contains('counter-btn')) return;

        const counter = btn.closest('.ticket-counter');
        const type = counter.dataset.ticketType;
        const action = btn.dataset.action;

        const children = parseInt(elements.childrenInput.value) || 0;
        const adult = parseInt(elements.adultInput.value) || 0;
        const senior = parseInt(elements.seniorInput.value) || 0;
        const oku = parseInt(document.querySelector('input[name="OkuCount"]')?.value) || 0;

        const currentTotal = children + adult + senior + oku;
        const delta = action === 'increment' ? 1 : -1;

        if (action === 'increment' && currentTotal >= state.selectedSeats.size) {
            return;
        }

        if (type === 'oku' && action === 'decrement') {
            const hasWheelchair = document.querySelector('.seat.selected.wheelchair');
            if (hasWheelchair && oku === 1) {
                alert("Cannot remove OKU ticket while a wheelchair seat is selected.");
                return;
            }
        }
        let targetInput = (type === 'oku') ? document.querySelector('input[name="OkuCount"]') : elements[`${type}Input`];
        let newValue = Math.max(0, (parseInt(targetInput.value) || 0) + delta);

        targetInput.value = newValue;
        counter.querySelector('[data-count-display]').textContent = newValue;

        calculatePricing();
    }

    function updateSeatCount() {
        const count = state.selectedSeats.size;
        if (elements.countDisplay) elements.countDisplay.textContent = count;
        if (elements.totalSeatsDisplay) elements.totalSeatsDisplay.textContent = count;

        if (count === 0) {
            elements.ticketTypeSection.style.display = 'none';
            resetCounters();
        } else {
            elements.ticketTypeSection.style.display = 'block';

            const children = parseInt(elements.childrenInput.value) || 0;
            const adult = parseInt(elements.adultInput.value) || 0;
            const senior = parseInt(elements.seniorInput.value) || 0;
            const oku = parseInt(document.querySelector('input[name="OkuCount"]')?.value) || 0;
            const currentTotalTickets = children + adult + senior + oku;

            if (currentTotalTickets < count) {
                const diff = count - currentTotalTickets;
                elements.adultInput.value = adult + diff;
                document.querySelector('[data-count-display="adult"]').textContent = adult + diff;
            }

            calculatePricing();
        }
    }

    function resetCounters() {
        if (elements.childrenInput) elements.childrenInput.value = 0;
        if (elements.adultInput) elements.adultInput.value = 0;
        if (elements.seniorInput) elements.seniorInput.value = 0;

        const okuInput = document.querySelector('input[name="OkuCount"]');
        if (okuInput) okuInput.value = 0;

        document.querySelectorAll('[data-count-display]').forEach(display => {
            display.textContent = 0;
        });
        document.querySelectorAll('[data-count-display]').forEach(display => {
            display.textContent = 0;
        });

        if (elements.totalDisplay) {
            elements.totalDisplay.textContent = '0.00';
        }
        if (elements.nextButton) {
            elements.nextButton.disabled = true;
        }
        if (elements.ticketWarning) {
            elements.ticketWarning.style.display = 'none';
        }
    }

    
   
    function releaseSeatsSync() {
        const xhr = new XMLHttpRequest();
        xhr.open('POST', '/Booking/ReleaseSeats', false); // Synchronous
        xhr.setRequestHeader('Content-Type', 'application/json');
        xhr.send(JSON.stringify({
            showTimeId: state.showTimeId,
            seatIds: Array.from(state.selectedSeats)
        }));
    }

    // Start countdown timer
    function startTimer() {
        stopTimer();

        if (elements.seatTimer) {
            elements.seatTimer.style.display = 'inline-block';
        }

        state.timerInterval = setInterval(() => {
            const now = new Date();
            const remaining = Math.max(0, state.lockExpiresAt - now);

            if (remaining <= 0) {
                handleTimerExpired();
                return;
            }

            const minutes = Math.floor(remaining / 60000);
            const seconds = Math.floor((remaining % 60000) / 1000);

            if (elements.timerDisplay) {
                elements.timerDisplay.textContent = `${minutes}:${seconds.toString().padStart(2, '0')}`;

                // Warning color when < 1 minute
                if (remaining < 60000) {
                    elements.timerDisplay.style.color = '#dc3545';
                } else {
                    elements.timerDisplay.style.color = '';
                }
            }
        }, 1000);
    }

    function stopTimer() {
        if (state.timerInterval) {
            clearInterval(state.timerInterval);
            state.timerInterval = null;
        }
        if (elements.seatTimer) {
            elements.seatTimer.style.display = 'none';
        }
    }


    function handleTimerExpired() {
        stopTimer();
        stopLockRenewal();
        alert('Your seat selection has expired. Please select again.');
        location.reload();
    }

    async function calculatePricing() {
        const children = parseInt(elements.childrenInput?.value) || 0;
        const adult = parseInt(elements.adultInput?.value) || 0;
        const senior = parseInt(elements.seniorInput?.value) || 0;
        const oku = parseInt(document.querySelector('input[name="OkuCount"]')?.value) || 0;

        if (state.selectedSeats.size === 0) {
            if (elements.totalDisplay) elements.totalDisplay.textContent = '0.00';
            if (elements.nextButton) elements.nextButton.disabled = true;
            return;
        }

        try {
            const response = await fetch('/Booking/CalculateTicketPrice', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    showTimeId: state.showTimeId,
                    childrenCount: children,
                    adultCount: adult,
                    seniorCount: senior,
                    okuCount: oku, // ADDED
                    selectedSeatsCount: state.selectedSeats.size,
                    selectedSeatIds: Array.from(state.selectedSeats) 
                })
            });

            const data = await response.json();

            if (data.success) {
                if (elements.totalDisplay) {
                    elements.totalDisplay.textContent = parseFloat(data.subtotal).toFixed(2);
                }

                const totalTickets = children + adult + senior + oku;
                const isValid = (totalTickets === state.selectedSeats.size);

                if (isValid) {
                    if (elements.ticketWarning) elements.ticketWarning.style.display = 'none';
                    if (elements.nextButton) elements.nextButton.disabled = false;
                } else {
                    if (elements.ticketWarning) elements.ticketWarning.style.display = 'block';
                    if (elements.nextButton) elements.nextButton.disabled = true;
                }
            }
        } catch (err) {
            console.error('Pricing error:', err);
        }
    }

    function handleFormSubmit(e) {
        if (state.selectedSeats.size === 0) {
            e.preventDefault();
            alert('Please select at least one seat');
            return;
        }

        const children = parseInt(elements.childrenInput.value) || 0;
        const adult = parseInt(elements.adultInput.value) || 0;
        const senior = parseInt(elements.seniorInput.value) || 0;
        const oku = parseInt(document.querySelector('input[name="OkuCount"]')?.value) || 0;

        const totalTickets = children + adult + senior + oku;

        if (totalTickets !== state.selectedSeats.size) {
            e.preventDefault();
            alert(`Ticket count (${totalTickets}) must match selected seats (${state.selectedSeats.size})`);
            return;
        }
        const container = document.getElementById('seatIdsContainer');
        if (container) {
            container.innerHTML = '';
            state.selectedSeats.forEach(seatId => {
                const input = document.createElement('input');
                input.type = 'hidden';
                input.name = 'SeatIds';
                input.value = seatId;
                container.appendChild(input);
            });

            const okuHidden = document.createElement('input');
            okuHidden.type = 'hidden';
            okuHidden.name = 'OkuCount';
            okuHidden.value = oku;
            container.appendChild(okuHidden);
        }

        stopTimer();
        stopLockRenewal();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();