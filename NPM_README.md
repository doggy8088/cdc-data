# @willh/html-text-node-parser

A lightweight utility for parsing text nodes from HTML strings or DOM elements.

## Installation

```bash
npm install @willh/html-text-node-parser
```

## Usage

### Parsing text nodes from HTML string

```typescript
import { parseTextNodes } from '@willh/html-text-node-parser';

const html = '<div><p>Hello</p><span>World</span></div>';
const textNodes = parseTextNodes(html);

console.log(textNodes);
// Output: [
//   { text: 'Hello', parentTag: 'p' },
//   { text: 'World', parentTag: 'span' }
// ]
```

### Parsing text nodes from DOM element

```typescript
import { parseTextNodesFromElement } from '@willh/html-text-node-parser';

const element = document.getElementById('myElement');
const textNodes = parseTextNodesFromElement(element);

console.log(textNodes);
```

## API

### `parseTextNodes(html: string): TextNode[]`

Extracts all text nodes from an HTML string.

**Parameters:**
- `html` - HTML string to parse

**Returns:**
- Array of `TextNode` objects containing text and parent tag information

**Note:** This function requires a browser environment (uses DOMParser).

### `parseTextNodesFromElement(element: Element): TextNode[]`

Extracts all text nodes from a DOM element.

**Parameters:**
- `element` - DOM element to parse

**Returns:**
- Array of `TextNode` objects containing text and parent tag information

### `TextNode` Interface

```typescript
interface TextNode {
  text: string;        // The text content (trimmed)
  parentTag: string;   // The parent element's tag name (lowercase)
}
```

## Features

- 🎯 Simple and focused API
- 📦 TypeScript support with full type definitions
- 🪶 Lightweight with no dependencies
- 🌐 Browser-only (requires DOM API)

## License

MIT

## Repository

https://github.com/doggy8088/cdc-data
