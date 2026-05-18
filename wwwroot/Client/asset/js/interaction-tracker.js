(function (window, document) {
    const endpoint = '/api/v1/user-interactions/track';

    function send(payload) {
        const body = JSON.stringify(payload);

        if (navigator.sendBeacon) {
            const blob = new Blob([body], { type: 'application/json' });
            navigator.sendBeacon(endpoint, blob);
            return;
        }

        fetch(endpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body,
            keepalive: true,
            credentials: 'same-origin'
        }).catch(function () { });
    }

    function buildMetadata(element) {
        const $el = window.jQuery ? window.jQuery(element) : null;
        const card = element.closest('.product-card, .product-card-ct, .card');
        let position = null;

        if ($el && card) {
            position = $el.closest('.product-card, .product-card-ct, .card').index() + 1;
        }

        return {
            slug: element.dataset.slug || null,
            placement: element.dataset.trackPlacement || null,
            query: element.dataset.trackQuery || null,
            section: element.closest('[data-section]')?.getAttribute('data-section') || null,
            wrapper: element.closest('[data-wrapper]')?.getAttribute('data-wrapper') || null,
            position,
            viewport: {
                width: window.innerWidth,
                height: window.innerHeight
            }
        };
    }

    function trackElement(element, overrides) {
        if (!element) return;

        const payload = {
            productId: element.dataset.productId ? Number(element.dataset.productId) : null,
            productSysId: element.dataset.productSysId || null,
            eventType: element.dataset.trackEvent || overrides?.eventType,
            source: element.dataset.trackSource || overrides?.source || null,
            metadata: Object.assign(buildMetadata(element), overrides?.metadata || {})
        };

        if (!payload.eventType || (!payload.productId && !payload.productSysId)) {
            return;
        }

        send(payload);
    }

    function track(payload) {
        if (!payload || !payload.eventType || (!payload.productId && !payload.productSysId)) {
            return;
        }
        send(payload);
    }

    window.UserInteractionTracker = {
        track: track,
        trackElement: trackElement
    };
})(window, document);
