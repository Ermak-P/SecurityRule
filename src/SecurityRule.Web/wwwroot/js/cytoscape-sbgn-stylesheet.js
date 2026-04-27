(function webpackUniversalModuleDefinition(root, factory) {
	if(typeof exports === 'object' && typeof module === 'object')
		module.exports = factory(require("lodash.memoize"), require("text-width"));
	else if(typeof define === 'function' && define.amd)
		define(["lodash.memoize", "text-width"], factory);
	else if(typeof exports === 'object')
		exports["cytoscapeSbgnStylesheet"] = factory(require("lodash.memoize"), require("text-width"));
	else
		root["cytoscapeSbgnStylesheet"] = factory(root["lodash.memoize"], root["text-width"]);
})(typeof self !== 'undefined' ? self : this, function(__WEBPACK_EXTERNAL_MODULE_5__, __WEBPACK_EXTERNAL_MODULE_10__) {
return /******/ (function(modules) { // webpackBootstrap
/******/ 	// The module cache
/******/ 	var installedModules = {};
/******/
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/
/******/ 		// Check if module is in cache
/******/ 		if(installedModules[moduleId]) {
/******/ 			return installedModules[moduleId].exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = installedModules[moduleId] = {
/******/ 			i: moduleId,
/******/ 			l: false,
/******/ 			exports: {}
/******/ 		};
/******/
/******/ 		// Execute the module function
/******/ 		modules[moduleId].call(module.exports, module, module.exports, __webpack_require__);
/******/
/******/ 		// Flag the module as loaded
/******/ 		module.l = true;
/******/
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/
/******/
/******/ 	// expose the modules object (__webpack_modules__)
/******/ 	__webpack_require__.m = modules;
/******/
/******/ 	// expose the module cache
/******/ 	__webpack_require__.c = installedModules;
/******/
/******/ 	// define getter function for harmony exports
/******/ 	__webpack_require__.d = function(exports, name, getter) {
/******/ 		if(!__webpack_require__.o(exports, name)) {
/******/ 			Object.defineProperty(exports, name, {
/******/ 				configurable: false,
/******/ 				enumerable: true,
/******/ 				get: getter
/******/ 			});
/******/ 		}
/******/ 	};
/******/
/******/ 	// getDefaultExport function for compatibility with non-harmony modules
/******/ 	__webpack_require__.n = function(module) {
/******/ 		var getter = module && module.__esModule ?
/******/ 			function getDefault() { return module['default']; } :
/******/ 			function getModuleExports() { return module; };
/******/ 		__webpack_require__.d(getter, 'a', getter);
/******/ 		return getter;
/******/ 	};
/******/
/******/ 	// Object.prototype.hasOwnProperty.call
/******/ 	__webpack_require__.o = function(object, property) { return Object.prototype.hasOwnProperty.call(object, property); };
/******/
/******/ 	// __webpack_public_path__
/******/ 	__webpack_require__.p = "";
/******/
/******/ 	// Load entry module and return exports
/******/ 	return __webpack_require__(__webpack_require__.s = 6);
/******/ })
/************************************************************************/
/******/ ([
/* 0 */
/***/ (function(module, exports) {

const sbgnDataHandler = {
  isMultimer (node) {
    return node.data('class').includes('multimer');
  },
  hasClonemarker (node) {
    return node.data('clonemarker');
  },
  getStateVars (node) {
    return node.data('stateVariables');
  },
  getUnitInfos (node) {
    return node.data('unitsOfInformation');
  },
  hasAuxItems (node) {
    return (node.data('stateVariables').length + node.data('unitsOfInformation').length > 0);
  },
  sbgnClass (element) {
    return element.data('class');
  },
  sbgnLabel (element) {
    return element.data('label');
  },
  stateVarLabel (stateVar) {
    const variable = stateVar.state.variable;
    const value = stateVar.state.value;
    if (value && variable) {
      return `${value}@${variable}`;
    }
    if (value) {
      return value;
    }

    if (variable) {
      return variable;
    }
    return '';
  }
};

module.exports = sbgnDataHandler;


/***/ }),
/* 1 */
/***/ (function(module, exports) {

const styleMap2Str = (styleMap) => {
  if( !styleMap ){
    return '';
  }

  let s = '';

  for( let [k, v] of styleMap ){
    s += k + '=' + '"' + v + '"' + ' ';
  }

  return s;
};

const svg = (svgStr, width = 100, height = 100) => {
  const parser = new DOMParser();
  let svgText =
  `<?xml version="1.0" encoding="UTF-8"?><!DOCTYPE svg><svg xmlns='http://www.w3.org/2000/svg' version='1.1' width='${width}' height='${height}'>${svgStr}</svg>`;
  return parser.parseFromString(svgText, 'text/xml').documentElement;
};

const svgStr = (svgText, viewPortWidth, viewPortHeight, viewBoxX, viewBoxY, viewBoxWidth, viewBoxHeight) => {
  let s = svg(svgText, viewPortWidth, viewPortHeight, viewBoxX, viewBoxY, viewBoxWidth, viewBoxHeight);

  // base64
  // let data = 'data:image/svg+xml;base64,' + btoa(s.outerHTML);

  // uri component string
  let data = 'data:image/svg+xml;utf8,' + encodeURIComponent(s.outerHTML);

  return data;
};

module.exports = {
  svgStr: svgStr,
  styleMap2Str: styleMap2Str
};


/***/ }),
/* 2 */
/***/ (function(module, exports, __webpack_require__) {

const styleMap2Str = __webpack_require__(1).styleMap2Str;

let baseRectangle = function (x, y, w, h, r1, r2, r3, r4, styleMap) {
  return `
  <path ${styleMap2Str(styleMap)} d='
    M ${x + r1} ${y}
    L ${x + w - r2} ${y} Q ${x + w} ${y} ${x + w} ${y + r2}
    L ${x + w } ${y + h - r3} Q ${x + w} ${y + h} ${x + w - r3} ${y + h}
    L ${x + r4} ${y + h} Q ${x} ${y + h} ${x} ${y + h - r4}
    L ${x} ${y + r1} Q ${x} ${y} ${x + r1} ${y}
    Z'
  />
  `;
};

const baseShapes = {
  barrel (x, y, width, height, styleMap) {
    return `

    <g ${styleMap2Str(styleMap)}>
      <path d="M ${0*width + x} ${.03*height + y} L ${0*width + x} ${.97*height + y} Q ${0.06*width + x} ${height + y} ${0.25*width + x} ${height + y}"/>

      <path d="M ${0.25*width + x} ${height + y} L ${0.75*width + x} ${height + y} Q ${0.95*width + x} ${height + y} ${width + x} ${0.95*height + y}"/>

      <path d="M ${width + x} ${.95*height + y} L ${width + x} ${0.05*height + y} Q ${width + x} ${0*height + y} ${0.75*width + x} ${0*height + y}"/>

      <path d="M ${0.75*width + x} ${0*height + y} L ${0.25*width + x} ${0*height + y} Q ${0.06*width + x} ${0*height + y} ${0*width + x} ${0.03*height + y}"/>
    </g>

    `;
  },

  circle (cx, cy, r, styleMap) {
    return `<circle cx='${cx}' cy='${cy}' r='${r}' ${styleMap2Str(styleMap)} />`;
  },

  clipPath (id, baseShapeFn, baseShapeFnArgs, styleMap) {
    return `
      <defs>
        <clipPath id='${id}' ${styleMap2Str(styleMap)}>
        ${baseShapeFn(...baseShapeFnArgs)}
        </clipPath>
      </defs>
    `;
  },

  concaveHexagon (x, y, width, height, styleMap) {
    return `
    <polygon ${styleMap2Str(styleMap)}
      points='${x + 0}, ${y + 0}, ${x + width}, ${y + 0}, ${x + 0.85*width}, ${y + 0.5*height}, ${x + width}, ${y + height}, ${x + 0}, ${y + height}, ${ x + 0.15*width}, ${y + 0.5*height}'
    />`;
  },

  cutRectangle (x, y, width, height, cornerLength, styleMap) {
    return `
    <polygon ${styleMap2Str(styleMap)}
      points='
      ${x + 0*width} ${y + cornerLength} ${x + cornerLength} ${y + 0*height} ${x + width - cornerLength} ${y + 0*height} ${x + width} ${y + cornerLength}
      ${x + width} ${y + height - cornerLength} ${x + width - cornerLength} ${y + height} ${x + cornerLength} ${y + height} ${x + 0*width} ${y + height - cornerLength}
      '
    />
    `;
  },

  ellipse (cx, cy, rx, ry, styleMap) {
    return `
      <ellipse cx='${cx}' cy='${cy}' rx='${rx}' ry='${ry}' ${styleMap2Str(styleMap)} />
    `;
  },

  hexagon (x, y, width, height, styleMap) {
    return `
    <polygon ${styleMap2Str(styleMap)}
      points='${x + 0}, ${y + 0.5*height}, ${x + 0.25*width}, ${y + 0*height}, ${x + 0.75*width}, ${y + 0*height}, ${x + width}, ${y + 0.5*height}, ${x + 0.75*width}, ${y + height}, ${x + 0.25*width}, ${y + height}'
    />`;
  },

  line (x1, y1, x2, y2, styleMap) {
    return `<line x1='${x1}' y1='${y1}' x2='${x2}' y2='${y2}' ${styleMap2Str(styleMap)} />`;
  },

  rectangle (x, y, width, height, styleMap) {
    return baseRectangle(x, y, width, height, 0, 0, 0, 0, styleMap);
  },

  roundBottomRectangle (x, y, width, height, styleMap) {
    return baseRectangle(x, y, width, height, 0, 0, .3*height, .3*height, styleMap);
  },

  roundRectangle (x, y, width, height, styleMap) {
    return baseRectangle(x, y, width, height, .04*width, .04*width, .04*width, .04*width, styleMap);
  },

  stadium (x, y, width, height, styleMap) {
    const radiusRatio = .24 * Math.max(width, height);
    return baseRectangle(x, y, width, height, radiusRatio, radiusRatio, radiusRatio, radiusRatio, styleMap);
  },

  square (x, y, length, styleMap) {
    return baseRectangle(x, y, length, length, 0, 0, 0, 0, styleMap);
  },

  text (t, x, y, styleMap) {
    return `<text x='${x}' y='${y}' ${styleMap2Str(styleMap)}>${t}</text>`;
  }

};


module.exports = baseShapes;


/***/ }),
/* 3 */
/***/ (function(module, exports, __webpack_require__) {

const sbgnData = __webpack_require__(0);

const sbgnStyle = new Map()
.set('unspecified entity', {w: 32, h: 32, shape: 'ellipse'})
.set('simple chemical', {w: 48, h: 48, shape: 'ellipse'})
.set('simple chemical multimer', {w: 48, h: 48, shape: 'ellipse'})
.set('macromolecule', {w: 96, h: 48, shape: 'roundrectangle'})
.set('macromolecule multimer', {w: 96, h: 48, shape: 'roundrectangle'})
.set('nucleic acid feature', {w: 88, h: 56, shape: 'bottomroundrectangle'})
.set('nucleic acid feature multimer', {w: 88, h: 52, shape: 'bottomroundrectangle'})
.set('complex', {w: 10, h: 10, shape: 'cutrectangle'})
.set('complex multimer', {w: 10, h: 10, shape: 'cutrectangle'})
.set('source and sink', {w: 60, h: 60, shape: 'polygon'})
.set('perturbing agent', {w: 140, h: 60, shape: 'concavehexagon'})

.set('phenotype', {w: 140, h: 60, shape: 'hexagon'})
.set('process', {w:25, h: 25, shape: 'square'})
.set('uncertain process', {w:25, h: 25, shape: 'square'})
.set('omitted process', {w:25, h: 25, shape: 'square'})
.set('association', {w:25, h: 25, shape: 'ellipse'})
.set('dissociation', {w:25, h: 25, shape: 'ellipse'})

.set('compartment', {w: 50, h: 50, shape: 'barrel'})

.set('tag', {w: 100, h: 65, shape: 'tag'})
.set('and', {w: 40, h: 40, shape: 'ellipse'})
.set('or', {w: 40, h: 40, shape: 'ellipse'})
.set('not', {w: 40, h: 40, shape: 'ellipse'});

const sbgnArrowMap = new Map()
.set('necessary stimulation', 'triangle-cross')
.set('inhibition', 'tee')
.set('catalysis', 'circle')
.set('stimulation', 'triangle')
.set('production', 'triangle')
.set('modulation', 'diamond');

const elementStyle = {
  sbgnShape (node) {
    const sbgnClass = sbgnData.sbgnClass(node).replace(' multimer', '');
    const style = sbgnStyle.get(sbgnClass);
    return style ? style.shape : 'ellipse';
  },

  sbgnArrowShape (edge) {
    const sbgnClass = sbgnData.sbgnClass(edge);
    const shape = sbgnArrowMap.get(sbgnClass);
    return shape ? shape : 'none';
  },

  sbgnContent (node) {
    const sbgnClass = sbgnData.sbgnClass(node).replace(' multimer', '');
    let content = sbgnData.sbgnLabel(node);

    if (sbgnClass == 'and') {
      content = 'AND';
    }
    if (sbgnClass == 'or') {
      content = 'OR';
    }
    if (sbgnClass == 'not') {
      content = 'NOT';
    }
    if (sbgnClass == 'omitted process') {
      content = '\\\\';
    }
    if (sbgnClass == 'uncertain process') {
      content = '?';
    }

    return content;
  },

  dimensions (node) {
    const sbgnClass = sbgnData.sbgnClass(node);
    const dim = sbgnStyle.get(sbgnClass);
    if (dim == null) {
      throw new TypeError(`${sbgnClass} does not have a default width / height`);
    }
    return dim;
  },

  width (node) {
    return this.dimensions(node).w;
  },

  height (node) {
    return this.dimensions(node).h;
  }
};

module.exports = elementStyle;


/***/ }),
/* 4 */
/***/ (function(module, exports, __webpack_require__) {

const textWidth = __webpack_require__(10);

const baseShapes = __webpack_require__(2);
const sbgnData = __webpack_require__(0);

const auxiliaryItems = {

  multiImgCloneMarker (x, y, width, height) {

    const cloneStyle = new Map()
    .set('stroke', '#6A6A6A')
    .set('stroke-width', '1')
    .set('fill', '#D2D2D2');

    return baseShapes.rectangle(x, y, width, height, cloneStyle);
  },

  multiImgUnitOfInformation (x, y, width, height, uInfo, borderWidth=3, fontSize=14) {
    const text = uInfo.label.text;
    const uinfoRectStyle = new Map()
    .set('stroke', '#555555')
    .set('stroke-width', `${borderWidth}`)
    .set('fill', 'white')
    .set('fill-opacity', 1);


    const textStyle = new Map()
    .set('alignment-baseline', 'middle')
    .set('font-size', `${fontSize}px`)
    .set('font-family', 'Helvetica Neue, Helvetica, sans-serif')
    .set('text-anchor', 'middle');

    const uInfoWidth = textWidth(text, { family: textStyle.get('font-family'), size: fontSize}) + 5;

    const unitOfInformationSvg =
    `
      ${baseShapes.roundRectangle(x, y, uInfoWidth, height, uinfoRectStyle)}
      ${baseShapes.text(text, x + (uInfoWidth / 2), y + ( height / 2),  textStyle)}
    `;

    return unitOfInformationSvg;
  },

  multiImgStateVar (x, y, width, height, stateVar, borderWidth=3, fontSize=14) {

    const stateVarStyle = new Map()
    .set('stroke', '#555555')
    .set('stroke-width', `${borderWidth}`)
    .set('fill', 'white')
    .set('fill-opacity', 1);

    const textStyle = new Map()
    .set('alignment-baseline', 'middle')
    .set('font-size', `${fontSize}px`)
    .set('font-family', 'Helvetica Neue, Helvetica, sans-serif')
    .set('text-anchor', 'middle');

    const tw = textWidth(sbgnData.stateVarLabel(stateVar), { family: textStyle.get('font-family'), size: fontSize}) + 10;
    const w = Math.max(tw, 30);
    const statevariableSvg =
    `
      ${baseShapes.stadium(x, y, w, height, stateVarStyle)}
      ${baseShapes.text(sbgnData.stateVarLabel(stateVar), x + ( w / 2 ), y + height / 2, textStyle)}
    `;

    return statevariableSvg;
  },

  cloneMarker (nodeWidth, nodeHeight, shapeFn, shapeFnArgs) {
    const clipId = 'clonemarker';

    const cloneMarkerStyle = new Map()
    .set('stroke', '#6A6A6A')
    .set('stroke-width', '1.5')
    .set('clip-path', `url(#${clipId})`)
    .set('fill', '#D2D2D2');

    const cloneMarkerSvg =
    `
      ${baseShapes.clipPath(clipId, baseShapes.rectangle,  [0, 3 * nodeHeight / 4, nodeWidth, nodeHeight, new Map()])}
      ${shapeFn(...shapeFnArgs, cloneMarkerStyle)}
    `;

    return cloneMarkerSvg;
  }
};

module.exports = auxiliaryItems;


/***/ }),
/* 5 */
/***/ (function(module, exports) {

module.exports = __WEBPACK_EXTERNAL_MODULE_5__;

/***/ }),
/* 6 */
/***/ (function(module, exports, __webpack_require__) {

let sbgnStyleSheet = __webpack_require__(7);

let defaultOptions = {
};

module.exports = function(cytoscape){
  return sbgnStyleSheet(cytoscape);
};


/***/ }),
/* 7 */
/***/ (function(module, exports, __webpack_require__) {

const elementStyle = __webpack_require__(3);
const sbgnsvg = __webpack_require__(8);

const sbgnStyleSheet = function (cytoscape) {

  return cytoscape.stylesheet()
        // general node style
        .selector('node')
        .css({
          'shape': (node) => elementStyle.sbgnShape(node),
          'content': (node) => elementStyle.sbgnContent(node),
          'font-size': 20,
          'width': (node) => elementStyle.width(node),
          'height': (node) => elementStyle.height(node),
          'text-valign': 'center',
          'text-halign': 'center',
          'border-width': 1.5,
          'border-color': '#555',
          'background-color': '#f6f6f6',
          'text-opacity': 1,
          'opacity': 1,
          'text-outline-color': 'white',
          'text-outline-opacity': 1,
          'text-outline-width': 0.75
        })
        .selector('node:selected')
        .css({
          'background-color': '#d67614',
          'target-arrow-color': '#000',
          'text-outline-color': '#000'
        })
        .selector('node:active')
        .css({
          'overlay-color': '#d67614',
          'overlay-padding': '14'
        })

        // draw sbgn specific styling (auxiliary items, clonemarker, etc.)
        .selector(`
          node[class="unspecified entity"],
          node[class="simple chemical"], node[class="simple chemical multimer"],
          node[class="macromolecule"], node[class="macromolecule multimer"],
          node[class="nucleic acid feature"], node[class="nucleic acid feature multimer"],
          node[class="perturbing agent"],
          node[class="phenotype"],
          node[class="complex"], node[class="complex multimer"], node[class="compartment"]
        `)
        .css({
          'background-image': (node) => sbgnsvg.draw(node).bgImage,
          'background-width': (node) => sbgnsvg.draw(node).bgWidth,
          'background-position-x': (node) => sbgnsvg.draw(node).bgPosX,
          'background-position-y': (node) => sbgnsvg.draw(node).bgPosY,
          'background-fit': (node) => sbgnsvg.draw(node).bgFit,
          'background-clip': (node) => sbgnsvg.draw(node).bgClip,
          'padding': (node) => sbgnsvg.draw(node).padding,
          'border-width': (node) => sbgnsvg.draw(node).borderWidth
        })

        .selector(`
          node[class="simple chemical multimer"],
          node[class="macromolecule multimer"],
          node[class="nucleic acid feature multimer"],
          node[class="complex multimer"]
        `)
        .css({
          'ghost': 'yes',
          'ghost-opacity': 1
        })

        .selector(`
          node[class="macromolecule multimer"],
          node[class="nucleic acid feature multimer"]
        `)
        .css({
          'ghost-offset-x': 12,
          'ghost-offset-y': 12
        })

        .selector(`
          node[class="simple chemical multimer"]
        `)
        .css({
          'ghost-offset-x': 5,
          'ghost-offset-y': 5
        })

        .selector(`
          node[class="complex multimer"]
        `)
        .css({
          'ghost-offset-x': 16,
          'ghost-offset-y': 16
        })

        // compound node specific style
        .selector('node[class="complex"], node[class="complex multimer"], node[class="compartment"]')
        .css({
          'compound-sizing-wrt-labels': 'exclude',
          'text-valign': 'bottom',
          'text-halign': 'center',
        })

        // process node specific style
        .selector('node[class="association"], node[class="dissociation"]')
        .css({
          'background-opacity': 1
        })
        .selector('node[class="association"]')
        .css({
          'background-color': '#6B6B6B'
        })

        // source and sink and dissociation are drawn differently because
        // of their unique shape
        .selector('node[class="source and sink"]')
        .css({
          'background-image': (node) => sbgnsvg.draw(node),
          'background-fit': 'none',
          'background-width': '100%',
          'background-height': '100%',
          'background-clip': 'none',
          'background-repeat': 'no-repeat',
          'border-width': 0,
          'shape-polygon-points': '-0.86, 0.5, -0.75, 0.65, -1, 0.95, -0.95, 1, -0.65, 0.75, -0.5, 0.86, 0, 1, 0.5, 0.86, 0.71, 0.71, 0.86, 0.5, 1, 0, 0.86, -0.5, 0.75, -0.65, 1, -0.95, 0.95, -1, 0.65, -0.75, 0.5, -0.86, 0, -1, -0.5, -0.86, -0.71, -0.71, -0.86, -0.5, -1, 0',
        })

        // source and sink and dissociation are drawn differently because
        // of their unique shape
        .selector('node[class="dissociation"]')
        .css({
          'background-image': (node) => sbgnsvg.draw(node),
          'background-fit': 'none',
          'background-width': '100%',
          'background-height': '100%',
          'background-clip': 'none',
          'background-repeat': 'no-repeat',
          'border-width': 0,
        })

        // edge styling
        .selector('edge')
        .css({
          'arrow-scale': 1.75,
          'curve-style': 'bezier',
          'line-color': '#555',
          'target-arrow-fill': 'hollow',
          'source-arrow-fill': 'hollow',
          'width': 1.5,
          'target-arrow-color': '#555',
          'source-arrow-color': '#555',
          'text-border-color': '#555',
          'color': '#555'
        })
        .selector('edge:selected')
        .css({
          'color': '#d67614',
          'line-color': '#d67614',
          'text-border-color': '#d67614',
          'source-arrow-color': '#d67614',
          'target-arrow-color': '#d67614'
        })
        .selector('edge:active')
        .css({
          'background-opacity': 0.7, 'overlay-color': '#d67614',
          'overlay-padding': '8'
        })
        .selector('edge[cardinality > 0]')
        .css({
          'text-background-shape': 'rectangle',
          'text-border-opacity': '1',
          'text-border-width': '1',
          'text-background-color': 'white',
          'text-background-opacity': '1'
        })
        .selector('edge[class="consumption"][cardinality > 0], edge[class="production"][cardinality > 0]')
        .css({
          'source-label': (edge) => '' + edge.data('cardinality'),
          'source-text-offset': 10
        })
        .selector('edge[class]')
        .css({
          'target-arrow-shape': (edge) => elementStyle.sbgnArrowShape(edge),
          'source-arrow-shape': 'none'
        })
        .selector('edge[class="inhibition"]')
        .css({
          'target-arrow-fill': 'filled'
        })
        .selector('edge[class="production"]')
        .css({
          'target-arrow-fill': 'filled'
        })


        // core
        .selector('core')
        .css({
          'selection-box-color': '#d67614',
          'selection-box-opacity': '0.2', 'selection-box-border-color': '#d67614'
        });
};

module.exports = sbgnStyleSheet;


/***/ }),
/* 8 */
/***/ (function(module, exports, __webpack_require__) {

const memoize = __webpack_require__(5);

const containerNodes = __webpack_require__(9);
const entityPoolNodes = __webpack_require__(11);
const processNodes = __webpack_require__(12);

const sbgnData = __webpack_require__(0);

const cacheKeyFn = (node) => '' + JSON.stringify(node.id());

const sbgnNodeShapeMap = new Map()
// process nodes
.set('dissociation', memoize(processNodes.dissociation, cacheKeyFn))
.set('phenotype', memoize(processNodes.phenotype, cacheKeyFn))

// entity pool nodes
.set('source and sink', memoize(entityPoolNodes.sourceAndSink, cacheKeyFn))
.set('unspecified entity', memoize(entityPoolNodes.unspecifiedEntity, cacheKeyFn))
.set('simple chemical', memoize(entityPoolNodes.simpleChemical, cacheKeyFn))
.set('macromolecule', memoize(entityPoolNodes.macromolecule, cacheKeyFn))
.set('nucleic acid feature', memoize(entityPoolNodes.nucleicAcidFeature, cacheKeyFn))
.set('complex', memoize(entityPoolNodes.complex, cacheKeyFn))
.set('perturbing agent', memoize(entityPoolNodes.perturbingAgent, cacheKeyFn))

// container nodes
.set('compartment', memoize(containerNodes.compartment, cacheKeyFn));


const draw = (node) => {
  const sbgnClass = sbgnData.sbgnClass(node).replace(' multimer', '');
  let shapeFn = sbgnNodeShapeMap.get(sbgnClass);
  if (shapeFn == null) {
    throw new TypeError(`${sbgnClass} does not have a shape implementation`);
  }
  return shapeFn(node);
};

module.exports = {
  draw: draw
};


/***/ }),
/* 9 */
/***/ (function(module, exports, __webpack_require__) {

const svgStr = __webpack_require__(1).svgStr;
const sbgnData = __webpack_require__(0);
const memoize = __webpack_require__(5);

const auxiliaryItems = __webpack_require__(4);
const baseShapes = __webpack_require__(2);

const containerNodes = {

  compartment (node) {
    const auxItemWidth = 60;
    const auxItemHeight = 40;
    const uInfos = sbgnData.getUnitInfos(node);

    const style = new Map()
    .set('stroke', '#555555')
    .set('stroke-width', '6');

    const uInfoSvg = svgStr(
      uInfos.length > 0 ? auxiliaryItems.multiImgUnitOfInformation(2, 0, auxItemWidth - 5, auxItemHeight - 3, uInfos[0]) : '',
      auxItemWidth, auxItemHeight
    );

    let lineSvg = svgStr(
      uInfos.length > 0 ? baseShapes.line(0, 0, auxItemWidth, 0, style) : '',
      auxItemWidth, auxItemHeight
    );

    return {
      bgImage: [lineSvg, uInfoSvg],
      bgWidth: ['100%'],
      bgPosX: ['0%', '25%'],
      bgPosY: ['19px', '0%'],
      bgFit: ['contain', 'none'],
      bgClip: 'node',
      padding: '38px',
      borderWidth: '4'
    };
  }
};

module.exports = containerNodes;


/***/ }),
/* 10 */
/***/ (function(module, exports) {

module.exports = __WEBPACK_EXTERNAL_MODULE_10__;

/***/ }),
/* 11 */
/***/ (function(module, exports, __webpack_require__) {

const baseShapes = __webpack_require__(2);
const auxiliaryItems = __webpack_require__(4);

const svgStr = __webpack_require__(1).svgStr;
const getUnitInfos = __webpack_require__(0).getUnitInfos;
const getStateVars = __webpack_require__(0).getStateVars;
const hasClonemarker = __webpack_require__(0).hasClonemarker;

const element = __webpack_require__(3);


const entityPoolNodes = {

  unspecifiedEntity (node) {
    const auxItemWidth = 100;
    const auxItemHeight = 20;
    const borderWidth = 2;
    const fontSize = 10;
    const uInfos = getUnitInfos(node);
    const sVars = getStateVars(node);

    const style = new Map()
    .set('stroke', '#6A6A6A')
    .set('stroke-width', '1');

    const cloneMarkerSvg = svgStr(
      hasClonemarker(node) ? auxiliaryItems.multiImgCloneMarker(0, 2, auxItemWidth, auxItemHeight - 3) : '',
      auxItemWidth, auxItemHeight
    );

    const uInfoSvg = svgStr(
      uInfos.length > 0 ? auxiliaryItems.multiImgUnitOfInformation(2, 0, auxItemWidth - 5, auxItemHeight - 3, uInfos[0], borderWidth, fontSize) : '',
      auxItemWidth, auxItemHeight
    );

    const sVarSvg = svgStr(
      sVars.length > 0 ? auxiliaryItems.multiImgStateVar(2, 0, auxItemWidth - 5, auxItemHeight - 3, sVars[0], borderWidth, fontSize) : '',
      auxItemWidth, auxItemHeight
    );

    const topLine = svgStr(
      uInfos.length + sVars.length > 0 ? baseShapes.line(0, 0, auxItemWidth, 0, style) : '',
      auxItemWidth, auxItemHeight
    );

    const bottomLine = svgStr(
      hasClonemarker(node) || uInfos.length > 0 ? baseShapes.line(0, 0, auxItemWidth, 0, style) : '',
      auxItemWidth, auxItemHeight
    );
    return {
      bgImage: [bottomLine, topLine, cloneMarkerSvg, uInfoSvg, sVarSvg],
      bgWidth: ['100%', '100%', '100%'],
      bgPosX: ['0%', '0%', '0%', '20px', '40px'],
      bgPosY: ['52px', '8px', '32px', '44px', '0%'],
      bgFit: ['cover', 'cover', 'none', 'none'],
      bgClip: 'node',
      padding: '8px',
      borderWidth: 2
    };

  },

  simpleChemical (node) {
    const auxItemWidth = 100;
    const auxItemHeight = 20;
    const borderWidth = 2;
    const fontSize = 10;
    const uInfos = getUnitInfos(node);

    const style = new Map()
    .set('stroke', '#6A6A6A')
    .set('stroke-width', '1');

    const cloneMarkerSvg = svgStr(
      hasClonemarker(node) ? auxiliaryItems.multiImgCloneMarker(0, 2, auxItemWidth, auxItemHeight - 3) : '',
      auxItemWidth, auxItemHeight
    );

    const uInfoSvg = svgStr(
      uInfos.length > 0 ? auxiliaryItems.multiImgUnitOfInformation(2, 0, auxItemWidth - 5, auxItemHeight - 3, uInfos[0], borderWidth, fontSize) : '',
      auxItemWidth, auxItemHeight
    );

    const topLine = svgStr(
      uInfos.length > 0 ? baseShapes.line(0, 0, auxItemWidth, 0, style) : '',
      auxItemWidth, auxItemHeight
    );

    const bottomLine = svgStr(
      hasClonemarker(node) || uInfos.length > 0 ? baseShapes.line(0, 0, auxItemWidth, 0, style) : '',
      auxItemWidth, auxItemHeight
    );

    return {
      bgImage: [bottomLine, topLine, cloneMarkerSvg, uInfoSvg],
      bgWidth: ['100%', '100%', '100%'],
      bgPosX: ['0%', '0%', '0%', '12px'],
      bgPosY: ['52px', '8px', '48px', '0px'],
      bgFit: ['cover', 'cover', 'none', 'none'],
      bgClip: 'node',
      padding: '8px',
      borderWidth: 2
    };
  },

  macromolecule(node) {
    const auxItemWidth = 100;
    const auxItemHeight = 20;
    const borderWidth = 2;
    const fontSize = 10;
    const uInfos = getUnitInfos(node);
    const sVars = getStateVars(node);

    const style = new Map()
    .set('stroke', '#6A6A6A')
    .set('stroke-width', '1');

    const cloneMarkerSvg = svgStr(
      hasClonemarker(node) ? auxiliaryItems.multiImgCloneMarker(0, 2, auxItemWidth, auxItemHeight - 3) : '',
      auxItemWidth, auxItemHeight
    );

    const uInfoSvg = svgStr(
      uInfos.length > 0 ? auxiliaryItems.multiImgUnitOfInformation(2, 0, auxItemWidth - 5, auxItemHeight - 3, uInfos[0], borderWidth, fontSize) : '',
      auxItemWidth, auxItemHeight
    );

    const sVarSvg = svgStr(
      sVars.length > 0 ? auxiliaryItems.multiImgStateVar(2, 0, auxItemWidth - 5, auxItemHeight - 3, sVars[0], borderWidth, fontSize) : '',
      auxItemWidth, auxItemHeight
    );

    const topLine = svgStr(
      uInfos.length + sVars.length > 0 ? baseShapes.line(0, 0, auxItemWidth, 0, style) : '',
      auxItemWidth, auxItemHeight
    );

    const bottomLine = svgStr(
      hasClonemarker(node) || uInfos.length > 0 ? baseShapes.line(0, 0, auxItemWidth, 0, style) : '',
      auxItemWidth, auxItemHeight
    );

    return {
      bgImage: [bottomLine, topLine, cloneMarkerSvg, uInfoSvg, sVarSvg],
      bgWidth: ['100%', '100%', '100%'],
      bgPosX: ['0%', '0%', '0%', '20px', '40px'],
      bgPosY: ['52px', '8px', '52px', '44px', '0%'],
      bgFit: ['cover', 'cover', 'none', 'none'],
      bgClip: 'node',
      padding: '8px',
      borderWidth: 2
    };  },

  nucleicAcidFeature (node) {
    const auxItemWidth = 100;
    const auxItemHeight = 20;
    const borderWidth = 2;
    const fontSize = 10;
    const uInfos = getUnitInfos(node);
    const sVars = getStateVars(node);

    const style = new Map()
    .set('stroke', '#6A6A6A')
    .set('stroke-width', '1');

    const cloneMarkerSvg = svgStr(
      hasClonemarker(node) ? auxiliaryItems.multiImgCloneMarker(0, 2, auxItemWidth, auxItemHeight - 3) : '',
      auxItemWidth, auxItemHeight
    );

    const uInfoSvg = svgStr(
      uInfos.length > 0 ? auxiliaryItems.multiImgUnitOfInformation(2, 0, auxItemWidth - 5, auxItemHeight - 3, uInfos[0], borderWidth, fontSize) : '',
      auxItemWidth, auxItemHeight
    );

    const sVarSvg = svgStr(
      sVars.length > 0 ? auxiliaryItems.multiImgStateVar(2, 0, auxItemWidth - 5, auxItemHeight - 3, sVars[0], borderWidth, fontSize) : '',
      auxItemWidth, auxItemHeight
    );

    const topLine = svgStr(
      sVars.length > 0 ? baseShapes.line(0, 0, auxItemWidth, 0, style) : '',
      auxItemWidth, auxItemHeight
    );

    const bottomLine = svgStr(
      hasClonemarker(node) || uInfos.length > 0 ? baseShapes.line(0, 0, auxItemWidth, 0, style) : '',
      auxItemWidth, auxItemHeight
    );

    return {
      bgImage: [bottomLine, topLine, cloneMarkerSvg, uInfoSvg, sVarSvg],
      bgWidth: ['100%', '100%', '100%'],
      bgPosX: ['0%', '0%', '0%', '20px', '40px'],
      bgPosY: ['52px', '8px', '52px', '44px', '0%'],
      bgFit: ['cover', 'cover', 'none', 'none'],
      bgClip: 'node',
      padding: '8px',
      borderWidth: 2
    };
  },

  complex (node) {
    const itemW = 60;
    const itemH = 24;
    const uInfos = getUnitInfos(node);
    const sVars = getStateVars(node);

    const images = [];
    const bgWidth = [];
    const bgHeight = [];
    const bgPosX = [];
    const bgPosY = [];
    const bgFit = [];

    const style = new Map()
    .set('stroke', '#555555')
    .set('stroke-width', '6');

    // order of svg image generation matters
    if (uInfos.length + sVars.length > 0) {
      const topLineSvg = svgStr(baseShapes.line(0, 0, itemW, 0, style), itemW, itemH);
      images.push(topLineSvg);
      bgWidth.push('100%');
      bgPosX.push('0%');
      bgPosY.push('11px');
      bgFit.push('none');
    }

    if (hasClonemarker(node)) {
      const bottomLineSvg = svgStr(baseShapes.line(0, 0, itemW, 0, style), itemW, itemH);
      images.push(bottomLineSvg);
      bgWidth.push('100%');
      bgPosX.push('0%');
      bgPosY.push('100%');
      bgFit.push('none');
    }

    if (hasClonemarker(node)) {
      const cloneSvg = svgStr(auxiliaryItems.multiImgCloneMarker(0, 2, itemW, itemH - 3), itemW, itemH);
      images.push(cloneSvg);
      bgWidth.push('100%');
      bgPosX.push('0%');
      bgPosY.push('100%');
      bgFit.push('none');
    }

    if (uInfos.length > 0) {
      const uInfoSvg = svgStr(auxiliaryItems.multiImgUnitOfInformation(2, 0, itemW - 5, itemH - 3, uInfos[0]), itemW, itemH);
      images.push(uInfoSvg);
      bgPosX.push('25%');
      bgPosY.push('0%');
      bgFit.push('none');
    }

    if (sVars.length > 0) {
      const sVarSvg = svgStr(auxiliaryItems.multiImgStateVar(2, 0, itemW - 5, itemH - 3, sVars[0]), itemW, itemH);
      images.push(sVarSvg);
      bgPosX.push('88%');
      bgPosY.push('0%');
      bgFit.push('none');
    }

    return {
      bgImage: images,
      bgWidth: bgWidth,
      bgPosX: bgPosX,
      bgPosY: bgPosY,
      bgFit: bgFit,
      bgClip: 'node',
      padding: '22px',
      borderWidth: 4
    };
  },

  sourceAndSink (node) {
    const {w: nw, h: nh} = element.dimensions(node);

    const centerX = nw / 2;
    const centerY = nh / 2;
    const radius = (nw - 2) / 2;

    const styleMap = new Map()
    .set('stroke', '#6A6A6A')
    .set('stroke-linecap', 'square')
    .set('stroke-width', '1.5')
    .set('fill', 'none');

    const shapeArgs = [centerX, centerY, radius];

    const sourceAndSinkSvg =
    `
      ${baseShapes.circle(...shapeArgs, styleMap)}
      ${hasClonemarker(node) ? auxiliaryItems.cloneMarker(nw, nh, baseShapes.circle, shapeArgs) : ''}
      ${baseShapes.line(0, nh, nw, 0, styleMap)}
    `;

    return svgStr(sourceAndSinkSvg, nw, nh, 0, 0, nw, nh);
  },

  perturbingAgent (node) {
    const auxItemWidth = 100;
    const auxItemHeight = 20;
    const borderWidth = 2;
    const fontSize = 10;
    const uInfos = getUnitInfos(node);

    const style = new Map()
    .set('stroke', '#6A6A6A')
    .set('stroke-width', '1');

    const cloneMarkerSvg = svgStr(
      hasClonemarker(node) ? auxiliaryItems.multiImgCloneMarker(0, 2, auxItemWidth, auxItemHeight - 3) : '',
      auxItemWidth, auxItemHeight
    );

    const uInfoSvg = svgStr(
      uInfos.length > 0 ? auxiliaryItems.multiImgUnitOfInformation(2, 0, auxItemWidth - 5, auxItemHeight - 3, uInfos[0], borderWidth, fontSize) : '',
      auxItemWidth, auxItemHeight
    );

    const topLine = svgStr(
      uInfos.length > 0 ? baseShapes.line(0, 0, auxItemWidth, 0, style) : '',
      auxItemWidth, auxItemHeight
    );

    const bottomLine = svgStr(
      hasClonemarker(node) || uInfos.length > 0 ? baseShapes.line(0, 0, auxItemWidth, 0, style) : '',
      auxItemWidth, auxItemHeight
    );

    return {
      bgImage: [bottomLine, topLine, cloneMarkerSvg, uInfoSvg],
      bgWidth: ['100%', '100%', '100%'],
      bgPosX: ['0%', '0%', '0%', '20px'],
      bgPosY: ['56px', '8px', '56px', '0%'],
      bgFit: ['cover', 'cover', 'none', 'none'],
      bgClip: 'node',
      padding: '8px',
      borderWidth: 2
    };
  }
};

module.exports = entityPoolNodes;


/***/ }),
/* 12 */
/***/ (function(module, exports, __webpack_require__) {

const baseShapes = __webpack_require__(2);
const auxiliaryItems = __webpack_require__(4);

const svgStr = __webpack_require__(1).svgStr;
const hasClonemarker = __webpack_require__(0).hasClonemarker;

const element = __webpack_require__(3);

const processNodes = {

  dissociation (node) {
    const {w: nw, h: nh} = element.dimensions(node);

    const centerX = nw / 2;
    const centerY = nh / 2;
    const outerRadius = (Math.min(nw, nh) - 2) / 2;
    const innerRadius = (Math.min(nw, nh) - 2) / 3;

    const styleMap = new Map()
    .set('stroke', '#6A6A6A')
    .set('stroke-width', '2')
    .set('fill', 'none');

    const dissociationSvg =
    `
      ${baseShapes.circle(centerX, centerY, outerRadius, styleMap)}
      ${baseShapes.circle(centerX, centerY, innerRadius, styleMap)}
    `;
    return svgStr(dissociationSvg, nw, nh);
  },

  phenotype (node) {
    const auxItemWidth = 100;
    const auxItemHeight = 20;

    const style = new Map()
    .set('stroke', '#6A6A6A')
    .set('stroke-width', '1');

    const cloneMarkerSvg = svgStr(
      hasClonemarker(node) ? auxiliaryItems.multiImgCloneMarker(0, 2, auxItemWidth, auxItemHeight - 3) : '',
      auxItemWidth, auxItemHeight, 0, 0, auxItemWidth, auxItemHeight
    );

    const bottomLine = svgStr(
      hasClonemarker(node) ? baseShapes.line(0, 0, auxItemWidth, 0, style) : '',
      auxItemWidth, auxItemHeight, 0, 0, auxItemWidth, auxItemHeight
    );

    return {
      bgImage: [bottomLine, cloneMarkerSvg],
      bgWidth: ['100%', '100%'],
      bgPosX: ['0%', '0%'],
      bgPosY: ['56px', '56px'],
      bgFit: ['cover', 'none'],
      bgClip: 'node',
      padding: '8px',
      borderWidth: 2
    };
  }
};

module.exports = processNodes;


/***/ })
/******/ ]);
});