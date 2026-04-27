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
        });

        cy.on('mouseout', 'edge', function (e) {
            e.target.animate(
                { style: { width: 1.5 } },
                { duration: 150, easing: 'ease-in-cubic' }
            );
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

    function getStyle() {
        /* Use cytoscape-sbgn-stylesheet as the base when available.
         * It provides:
         *   • White text-outline for all nodes (improves label readability)
         *   • Foundation styles for SBGN glyph classes
         *   • Hollow-arrow-capable edge defaults
         * We then override shape/color/size selectors to match the
         * network-diagram visual language of this application.
         */
        const base = typeof cytoscapeSbgnStylesheet !== 'undefined'
            ? cytoscapeSbgnStylesheet(cytoscape)
            : cytoscape.stylesheet();

        return base
            /* ── Server compound node (compartment) ── */
            .selector('node[type="server"]')
            .css({
                'shape': 'round-rectangle',
                'background-color': '#e8f4fc',
                'background-opacity': 0.65,
                'border-color': '#1565c0',
                'border-width': 2,
                'border-style': 'solid',
                'background-image': 'none',
                'label': 'data(label)',
                'content': 'data(label)',
                'text-valign': 'top',
                'text-halign': 'center',
                'font-size': '13px',
                'font-weight': 'bold',
                'font-family': 'Roboto, sans-serif',
                'color': '#0d47a1',
                'text-wrap': 'wrap',
                'text-max-width': '180px',
                'text-margin-y': 8,
                'padding': '28px',
                'width': 'label',
                'height': 'label',
                'min-width': 150,
                'min-height': 70
            })
            /* ── Related (dimmed) server ── */
            .selector('node[type="server"][dimmed="1"]')
            .css({
                'background-color': '#f5f5f5',
                'background-opacity': 0.35,
                'border-color': '#b0bec5',
                'border-width': 1.5,
                'border-style': 'dashed',
                'color': '#78909c',
                'font-weight': 'normal'
            })
            /* ── Service node (macromolecule) ── */
            .selector('node[type="service"]')
            .css({
                'shape': 'round-rectangle',
                'background-color': '#ffffff',
                'background-opacity': 1,
                'border-color': '#42a5f5',
                'border-width': 1.5,
                'border-style': 'solid',
                'background-image': 'none',
                'label': 'data(label)',
                'content': 'data(label)',
                'text-valign': 'center',
                'text-halign': 'center',
                'font-size': '11px',
                'font-family': 'Roboto, sans-serif',
                'color': '#1a237e',
                'text-wrap': 'wrap',
                'text-max-width': '115px',
                'width': 130,
                'height': 36,
                'padding': '8px'
            })
            /* ── Service inside a dimmed server ── */
            .selector('node[type="service"][dimmed="1"]')
            .css({
                'background-color': '#fafafa',
                'border-color': '#b0bec5',
                'color': '#90a4ae'
            })
            /* ── Edges ── */
            .selector('edge')
            .css({
                'curve-style': 'bezier',
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
            })
            /* ── Edge from service (teal) ── */
            .selector('edge[fromService="1"]')
            .css({
                'line-color': '#00796b',
                'target-arrow-color': '#00796b'
            })
            /* ── Selection highlights ── */
            .selector('node:selected')
            .css({
                'border-color': '#f57c00',
                'border-width': 3
            })
            .selector('edge:selected')
            .css({
                'line-color': '#f57c00',
                'target-arrow-color': '#f57c00',
                'width': 2.5
            });
    }

    return { init: init, update: update };
}());
