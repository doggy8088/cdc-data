/**
 * Parse HTML text nodes from a given HTML string or DOM element
 */

export interface TextNode {
  text: string;
  parentTag: string;
}

/**
 * Extract text nodes from HTML string
 * @param html - HTML string to parse
 * @returns Array of text nodes with their parent tags
 */
export function parseTextNodes(html: string): TextNode[] {
  if (typeof window === 'undefined') {
    throw new Error('This function requires a browser environment');
  }

  const parser = new DOMParser();
  const doc = parser.parseFromString(html, 'text/html');
  const textNodes: TextNode[] = [];

  function traverse(node: Node) {
    if (node.nodeType === Node.TEXT_NODE) {
      const text = node.textContent?.trim();
      if (text) {
        textNodes.push({
          text,
          parentTag: (node.parentElement?.tagName || 'unknown').toLowerCase(),
        });
      }
    } else {
      node.childNodes.forEach(traverse);
    }
  }

  traverse(doc.body);
  return textNodes;
}

/**
 * Extract text nodes from a DOM element
 * @param element - DOM element to parse
 * @returns Array of text nodes with their parent tags
 */
export function parseTextNodesFromElement(element: Element): TextNode[] {
  const textNodes: TextNode[] = [];

  function traverse(node: Node) {
    if (node.nodeType === Node.TEXT_NODE) {
      const text = node.textContent?.trim();
      if (text) {
        textNodes.push({
          text,
          parentTag: (node.parentElement?.tagName || 'unknown').toLowerCase(),
        });
      }
    } else {
      node.childNodes.forEach(traverse);
    }
  }

  traverse(element);
  return textNodes;
}

export default {
  parseTextNodes,
  parseTextNodesFromElement,
};
