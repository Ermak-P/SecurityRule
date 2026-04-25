window.graphMap = (function () {
    'use strict';

    let cy = null;

    function init(containerId, elements) {
        const container = document.getElementById(containerId);
        if (!container) return;

        if (cy) {
            cy.destroy();
            cy = null;
        }

        cy = cytoscape({
            container: container,
            elements: elements,
            style: getStyle(),
            layout: getLayout()
        });
    }

    function update(elements) {
        if (!cy) return;
        cy.elements().remove();
        cy.add(elements);
        cy.layout(getLayout()).run();
    }

    function getLayout() {
        return {
            name: 'fcose',
            animate: false,
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
        return [
            /* ── Server compound node (SBGN compartment-like) ── */
            {
                selector: 'node[type="server"]',
                style: {
                    'shape': 'round-rectangle',
                    'background-color': '#e8f4fc',
                    'background-opacity': 0.65,
                    'border-color': '#1565c0',
                    'border-width': 2,
                    'label': 'data(label)',
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
                    'min-width': 150,
                    'min-height': 70
                }
            },
            /* ── Service node (SBGN macromolecule / process-like) ── */
            {
                selector: 'node[type="service"]',
                style: {
                    'shape': 'round-rectangle',
                    'background-color': '#ffffff',
                    'border-color': '#42a5f5',
                    'border-width': 1.5,
                    'label': 'data(label)',
                    'text-valign': 'center',
                    'text-halign': 'center',
                    'font-size': '11px',
                    'font-family': 'Roboto, sans-serif',
                    'color': '#1a237e',
                    'text-wrap': 'wrap',
                    'text-max-width': '115px',
                    'width': 130,
                    'height': 36
                }
            },
            /* ── Default edge ── */
            {
                selector: 'edge',
                style: {
                    'curve-style': 'bezier',
                    'target-arrow-shape': 'triangle',
                    'target-arrow-color': '#1565c0',
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
            /* ── Hover / selected highlights ── */
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
