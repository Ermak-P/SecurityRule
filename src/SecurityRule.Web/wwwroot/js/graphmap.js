if (typeof cytoscapeFcose !== 'undefined') {
    cytoscape.use(cytoscapeFcose);
}

window.graphMap = (function () {
    'use strict';

    let cy = null;

    const ANIM_DURATION = 600;
    const ANIM_EASING   = 'ease-in-out-cubic';

    function init(containerId, elements) {
        const container = document.getElementById(containerId);
        if (!container) return;

        if (cy) {
            cy.destroy();
            cy = null;
        }

        createTooltip();
        hideTooltip();

        /* Start nodes invisible so we can fade them in after layout */
        const invisible = elements.map(function (el) {
            return Object.assign({}, el, {
                data: Object.assign({}, el.data),
                style: Object.assign({}, el.style, { opacity: 0 })
            });
        });

        cy = cytoscape({
            container: container,
            elements: invisible,
            style: getStyle(),
            layout: { name: 'preset' }   /* position-less placeholder */
        });

        bindHoverEvents();
        bindInteractionEvents();

        cy.layout(getLayout()).run();
    }

    function update(elements) {
        if (!cy) return;

        /* Build lookup for incoming elements by id */
        const incomingIds = new Set(elements.map(function (el) { return el.data.id; }));

        /* Elements to remove: currently in cy but absent from new set */
        const toRemove = cy.elements().filter(function (el) { return !incomingIds.has(el.id()); });

        /* Elements to add: in incoming but not yet in cy */
        const existingIds = new Set();
        cy.elements().forEach(function (el) { existingIds.add(el.id()); });
        const toAdd = elements.filter(function (el) { return !existingIds.has(el.data.id); });

        /* Update data for elements that already exist (handles dimmed attribute changes) */
        elements.forEach(function (el) {
            if (existingIds.has(el.data.id)) {
                cy.getElementById(el.data.id).data(el.data);
            }
        });

        if (toRemove.length === 0 && toAdd.length === 0) return;

        function applyLayout() {
            if (toAdd.length > 0) {
                const invisible = toAdd.map(function (el) {
                    return Object.assign({}, el, {
                        data: Object.assign({}, el.data),
                        style: Object.assign({}, el.style, { opacity: 0 })
                    });
                });
                cy.add(invisible);
            }
            cy.layout(getLayout()).run();
        }

        if (toRemove.length > 0) {
            toRemove.animate({ style: { opacity: 0 } }, {
                duration: ANIM_DURATION / 2,
                easing: ANIM_EASING,
                complete: function () {
                    toRemove.remove();
                    applyLayout();
                }
            });
        } else {
            applyLayout();
        }
    }

    function bindHoverEvents() {
        cy.on('mouseover', 'node', function (e) {
            e.target.animate(
                { style: { 'border-width': 3, 'background-opacity': 0.9 } },
                { duration: 180, easing: 'ease-out-cubic' }
            );
        });

        cy.on('mouseout', 'node', function (e) {
            const type   = e.target.data('type');
            const dimmed = e.target.data('dimmed') === '1';
            e.target.animate(
                {
                    style: {
                        'border-width':       type === 'server' ? (dimmed ? 1.5 : 2) : 1.5,
                        'background-opacity': type === 'server' ? (dimmed ? 0.35 : 0.65) : 1
                    }
                },
                { duration: 180, easing: 'ease-in-cubic' }
            );
        });

        cy.on('mouseover', 'edge', function (e) {
            e.target.animate(
                { style: { width: 2.5 } },
                { duration: 150, easing: 'ease-out-cubic' }
            );
            if (tooltip) {
                var desc = e.target.data('description');
                if (desc) {
                    tooltip.textContent = desc;
                    tooltip.style.display = 'block';
                }
            }
        });

        cy.on('mousemove', 'edge', function (e) {
            if (tooltip && tooltip.style.display !== 'none') {
                var evt = e.originalEvent;
                if (evt) {
                    tooltip.style.left = (evt.clientX + 14) + 'px';
                    tooltip.style.top  = (evt.clientY - 30) + 'px';
                }
            }
        });

        cy.on('mouseout', 'edge', function (e) {
            e.target.animate(
                { style: { width: 1.5 } },
                { duration: 150, easing: 'ease-in-cubic' }
            );
            hideTooltip();
        });
    }

    function bindInteractionEvents() {
        /* Show pointer cursor on nodes (they are double-clickable) */
        cy.on('mouseover', 'node', function () {
            cy.container().style.cursor = 'pointer';
        });
        cy.on('mouseout', 'node', function () {
            cy.container().style.cursor = '';
        });

        /* Double-click: navigate to server or service details page */
        cy.on('dbltap', 'node', function (e) {
            var data = e.target.data();
            var rawId, numId;
            if (data.type === 'server') {
                rawId = String(data.id).replace('srv-', '');
                numId = parseInt(rawId, 10);
                if (numId > 0) window.location.href = '/servers/' + numId;
            } else if (data.type === 'service') {
                rawId = String(data.id).replace('svc-', '');
                numId = parseInt(rawId, 10);
                if (numId > 0) window.location.href = '/services/' + numId;
            }
        });
    }

    function getLayout() {
        return {
            name: 'fcose',
            animate: true,
            animationDuration: ANIM_DURATION,
            animationEasing: ANIM_EASING,
            /* Fade elements in once layout animation finishes */
            ready: function () {
                if (cy) {
                    cy.elements().animate(
                        { style: { opacity: 1 } },
                        { duration: ANIM_DURATION / 2, easing: ANIM_EASING }
                    );
                }
            },
            quality: 'proof',
            randomize: true,
            fit: true,
            padding: 40,
            nodeDimensionsIncludeLabels: true,
            uniformNodeDimensions: false,
            packComponents: true,
            nodeRepulsion: 8000,
            idealEdgeLength: 120,
            edgeElasticity: 0.45,
            nestingFactor: 0.1,
            gravity: 0.25,
            gravityRange: 3.8,
            gravityCompound: 1.0,
            gravityRangeCompound: 1.5,
            numIter: 2500,
            tile: true,
            tilingPaddingVertical: 10,
            tilingPaddingHorizontal: 10
        };
    }

    /* ── Material Design icons (Dns / MiscellaneousServices) as SVG data URIs ── */
    var ICON_SERVER = 'data:image/svg+xml,' + encodeURIComponent(
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">' +
        '<path d="M20 13H4c-.55 0-1 .45-1 1v6c0 .55.45 1 1 1h16c.55 0 1-.45 1-1v-6c0-.55-.45-1-1-1z' +
        'M7 19c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2z' +
        'M20 3H4c-.55 0-1 .45-1 1v6c0 .55.45 1 1 1h16c.55 0 1-.45 1-1V4c0-.55-.45-1-1-1z' +
        'M7 9c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2z" fill="#1565c0"/>' +
        '</svg>'
    );

    var ICON_APP = 'data:image/svg+xml,' + encodeURIComponent(
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">' +
        '<path d="M14.17,13.71l1.4-2.42c0.09-0.15,0.05-0.34-0.08-0.45l-1.48-1.16' +
        'c0.03-0.22,0.05-0.45,0.05-0.68s-0.02-0.46-0.05-0.69l1.48-1.16c0.13-0.11,0.17-0.3,0.08-0.45l-1.4-2.42' +
        'c-0.09-0.15-0.27-0.21-0.43-0.15L12,4.83c-0.36-0.28-0.75-0.51-1.18-0.69l-0.26-1.85C10.53,2.13,10.38,2,10.21,2h-2.8' +
        'C7.24,2,7.09,2.13,7.06,2.3L6.8,4.15C6.38,4.33,5.98,4.56,5.62,4.84l-1.74-0.7c-0.16-0.06-0.34,0-0.43,0.15l-1.4,2.42' +
        'C1.96,6.86,2,7.05,2.13,7.16l1.48,1.16C3.58,8.54,3.56,8.77,3.56,9s0.02,0.46,0.05,0.69l-1.48,1.16' +
        'C2,10.96,1.96,11.15,2.05,11.3l1.4,2.42c0.09,0.15,0.27,0.21,0.43,0.15l1.74-0.7c0.36,0.28,0.75,0.51,1.18,0.69' +
        'l0.26,1.85C7.09,15.87,7.24,16,7.41,16h2.8c0.17,0,0.32-0.13,0.35-0.3l0.26-1.85c0.42-0.18,0.82-0.41,1.18-0.69' +
        'l1.74,0.7C13.9,13.92,14.08,13.86,14.17,13.71z' +
        'M8.81,11c-1.1,0-2-0.9-2-2c0-1.1,0.9-2,2-2s2,0.9,2,2C10.81,10.1,9.91,11,8.81,11z" fill="#1565c0"/>' +
        '<path d="M21.92,18.67l-0.96-0.74c0.02-0.14,0.04-0.29,0.04-0.44c0-0.15-0.01-0.3-0.04-0.44l0.95-0.74' +
        'c0.08-0.07,0.11-0.19,0.05-0.29l-0.9-1.55c-0.05-0.1-0.17-0.13-0.28-0.1l-1.11,0.45c-0.23-0.18-0.48-0.33-0.76-0.44' +
        'l-0.17-1.18C18.73,13.08,18.63,13,18.53,13h-1.79c-0.11,0-0.21,0.08-0.22,0.19l-0.17,1.18' +
        'c-0.27,0.12-0.53,0.26-0.76,0.44l-1.11-0.45c-0.1-0.04-0.22,0-0.28,0.1l-0.9,1.55c-0.05,0.1-0.04,0.22,0.05,0.29' +
        'l0.95,0.74c-0.02,0.14-0.03,0.29-0.03,0.44c0,0.15,0.01,0.3,0.03,0.44l-0.95,0.74c-0.08,0.07-0.11,0.19-0.05,0.29' +
        'l0.9,1.55c0.05,0.1,0.17,0.13,0.28,0.1l1.11-0.45c0.23,0.18,0.48,0.33,0.76,0.44l0.17,1.18' +
        'c0.02,0.11,0.11,0.19,0.22,0.19h1.79c0.11,0,0.21-0.08,0.22-0.19l0.17-1.18c0.27-0.12,0.53-0.26,0.75-0.44' +
        'l1.12,0.45c0.1,0.04,0.22,0,0.28-0.1l0.9-1.55C22.03,18.86,22,18.74,21.92,18.67z' +
        'M17.63,18.83c-0.74,0-1.35-0.6-1.35-1.35s0.6-1.35,1.35-1.35s1.35,0.6,1.35,1.35S18.37,18.83,17.63,18.83z" fill="#1565c0"/>' +
        '</svg>'
    );

    let tooltip = null;

    function createTooltip() {
        if (tooltip) return;
        tooltip = document.createElement('div');
        tooltip.style.cssText =
            'position:fixed;background:rgba(55,71,79,0.93);color:#fff;' +
            'padding:5px 9px;border-radius:4px;font-size:11px;' +
            'font-family:Roboto,sans-serif;pointer-events:none;z-index:9999;' +
            'display:none;max-width:280px;word-wrap:break-word;line-height:1.4;' +
            'box-shadow:0 2px 6px rgba(0,0,0,0.28);';
        document.body.appendChild(tooltip);
    }

    function hideTooltip() {
        if (tooltip) tooltip.style.display = 'none';
    }

    function getStyle() {
        return [
            /* ── Server compound node ── */
            {
                selector: 'node[type="server"]',
                style: {
                    'shape': 'round-rectangle',
                    'background-color': '#e8f4fc',
                    'background-opacity': 0.65,
                    'border-color': '#1565c0',
                    'border-width': 2,
                    'border-style': 'solid',
                    'label': 'data(label)',
                    'content': 'data(label)',
                    'text-valign': 'top',
                    'text-halign': 'center',
                    'font-size': '13px',
                    'font-weight': 'bold',
                    'font-family': 'Roboto, sans-serif',
                    'color': '#0d47a1',
                    'text-outline-color': 'white',
                    'text-outline-width': '0.75',
                    'text-wrap': 'wrap',
                    'text-max-width': '180px',
                    'text-margin-y': 8,
                    'padding': '28px',
                    'width': 'label',
                    'height': 'label',
                    'min-width': 150,
                    'min-height': 70
                }
            },
            /* ── Server icon (Dns / rack icon, top-left badge) ── */
            {
                selector: 'node[nodeType="server"]',
                style: {
                    'background-image': ICON_SERVER,
                    'background-width': '18px',
                    'background-height': '18px',
                    'background-fit': 'none',
                    'background-position-x': '6px',
                    'background-position-y': '6px',
                    'background-clip': 'none'
                }
            },
            /* ── Related (dimmed) server ── */
            {
                selector: 'node[type="server"][dimmed="1"]',
                style: {
                    'background-color': '#f5f5f5',
                    'background-opacity': 0.35,
                    'border-color': '#b0bec5',
                    'border-width': 1.5,
                    'border-style': 'dashed',
                    'color': '#78909c',
                    'font-weight': 'normal',
                    'background-image-opacity': 0.45
                }
            },
            /* ── Service node ── */
            {
                selector: 'node[type="service"]',
                style: {
                    'shape': 'round-rectangle',
                    'background-color': '#ffffff',
                    'background-opacity': 1,
                    'border-color': '#42a5f5',
                    'border-width': 1.5,
                    'border-style': 'solid',
                    'label': 'data(label)',
                    'content': 'data(label)',
                    'text-valign': 'center',
                    'text-halign': 'center',
                    'font-size': '11px',
                    'font-family': 'Roboto, sans-serif',
                    'color': '#1a237e',
                    'text-outline-color': 'white',
                    'text-outline-width': '0.75',
                    'text-wrap': 'wrap',
                    'text-max-width': '95px',
                    'text-margin-x': 10,
                    'width': 130,
                    'height': 36,
                    'padding': '8px'
                }
            },
            /* ── Service icon (MiscellaneousServices, left-center) ── */
            {
                selector: 'node[nodeType="app"]',
                style: {
                    'background-image': ICON_APP,
                    'background-width': '16px',
                    'background-height': '16px',
                    'background-fit': 'none',
                    'background-position-x': '7px',
                    'background-position-y': '50%',
                    'background-clip': 'none'
                }
            },
            /* ── Service inside a dimmed server ── */
            {
                selector: 'node[type="service"][dimmed="1"]',
                style: {
                    'background-color': '#fafafa',
                    'border-color': '#b0bec5',
                    'color': '#90a4ae',
                    'background-image-opacity': 0.45
                }
            },
            /* ── Edges ── */
            {
                selector: 'edge',
                style: {
                    'curve-style': 'bezier',
                    'control-point-step-size': 60,
                    'target-arrow-shape': 'triangle',
                    'target-arrow-fill': 'filled',
                    'target-arrow-color': '#1565c0',
                    'source-arrow-shape': 'none',
                    'line-color': '#1565c0',
                    'width': 1.5,
                    'label': 'data(label)',
                    'font-size': '9px',
                    'font-family': 'Roboto, sans-serif',
                    'color': '#37474f',
                    'text-background-color': '#ffffff',
                    'text-background-opacity': 0.9,
                    'text-background-padding': '2px',
                    'text-background-shape': 'round-rectangle',
                    'text-rotation': 'autorotate'
                }
            },
            /* ── Edge from service (teal) ── */
            {
                selector: 'edge[fromService="1"]',
                style: {
                    'line-color': '#00796b',
                    'target-arrow-color': '#00796b'
                }
            },
            /* ── Selection highlights ── */
            {
                selector: 'node:selected',
                style: {
                    'border-color': '#f57c00',
                    'border-width': 3
                }
            },
            {
                selector: 'edge:selected',
                style: {
                    'line-color': '#f57c00',
                    'target-arrow-color': '#f57c00',
                    'width': 2.5
                }
            }
        ];
    }

    return { init: init, update: update };
}());
