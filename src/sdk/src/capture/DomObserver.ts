import {
  BaseEvent,
  EventType,
  SerializedNode,
  SerializedMutation,
} from "../types/events";
import { EventEmitter } from "./MouseTracker";
import { shouldExclude, shouldMask, maskValue } from "../privacy/Masking";

export class DomObserver {
  private emit: EventEmitter;
  private sessionId: () => string;
  private maskAllInputs: boolean;
  private observer: MutationObserver | null = null;
  private batchTimer: ReturnType<typeof setTimeout> | null = null;
  private pendingMutations: SerializedMutation[] = [];

  constructor(
    emit: EventEmitter,
    sessionId: () => string,
    maskAllInputs: boolean,
  ) {
    this.emit = emit;
    this.sessionId = sessionId;
    this.maskAllInputs = maskAllInputs;
  }

  start(): void {
    this.captureSnapshot();

    this.observer = new MutationObserver((mutations) => {
      this.processMutations(mutations);
    });

    this.observer.observe(document.documentElement, {
      childList: true,
      subtree: true,
      attributes: true,
      attributeOldValue: true,
      characterData: true,
      characterDataOldValue: true,
    });

    this.observeResize();
  }

  stop(): void {
    if (this.observer) {
      this.observer.disconnect();
      this.observer = null;
    }
    if (this.batchTimer) {
      clearTimeout(this.batchTimer);
      this.flushMutations();
    }
  }

  captureSnapshot(): void {
    const doctype = document.doctype;
    const doctypeStr = doctype
      ? `<!DOCTYPE ${doctype.name}${doctype.publicId ? ` PUBLIC "${doctype.publicId}"` : ""}${doctype.systemId ? ` "${doctype.systemId}"` : ""}>`
      : "<!DOCTYPE html>";

    const clone = document.documentElement.cloneNode(true) as HTMLElement;
    this.sanitizeNode(clone);

    const html = doctypeStr + clone.outerHTML;

    this.emit({
      sessionId: this.sessionId(),
      timestamp: Date.now(),
      type: EventType.DomSnapshot,
      pageUrl: location.href,
      data: {
        html,
        width: window.innerWidth,
        height: window.innerHeight,
        href: location.href,
      },
    });
  }

  private sanitizeNode(node: Element): void {
    if (shouldExclude(node)) {
      node.innerHTML = "<!-- di-excluded -->";
      return;
    }

    // Mask script contents
    if (node.tagName === "SCRIPT") {
      node.textContent = "";
      return;
    }

    if (
      node instanceof HTMLInputElement ||
      node instanceof HTMLTextAreaElement
    ) {
      if (shouldMask(node, this.maskAllInputs)) {
        node.setAttribute("value", maskValue(node.value));
      }
    }

    for (const child of Array.from(node.children)) {
      this.sanitizeNode(child);
    }
  }

  private processMutations(mutations: MutationRecord[]): void {
    for (const mutation of mutations) {
      const target = mutation.target as Element;
      if (!target || shouldExclude(target)) continue;

      const serialized: SerializedMutation = {
        type: mutation.type as SerializedMutation["type"],
        target: this.getNodeDescriptor(target),
        targetSelector: this.getSelector(target),
      };

      if (mutation.type === "childList") {
        if (mutation.addedNodes.length > 0) {
          serialized.addedNodes = Array.from(mutation.addedNodes)
            .filter((n) => n.nodeType === 1 || n.nodeType === 3)
            .slice(0, 20) // Limit to prevent massive payloads
            .map((n) => this.serializeNode(n));
        }
        if (mutation.removedNodes.length > 0) {
          serialized.removedNodes = Array.from(mutation.removedNodes)
            .filter((n) => n.nodeType === 1)
            .slice(0, 20)
            .map((n) => this.getNodeDescriptor(n as Element));
        }
      } else if (mutation.type === "attributes") {
        serialized.attributeName = mutation.attributeName || undefined;
        serialized.attributeValue = mutation.attributeName
          ? target.getAttribute(mutation.attributeName)
          : null;
        serialized.oldValue = mutation.oldValue;
      } else if (mutation.type === "characterData") {
        serialized.oldValue = mutation.oldValue;
      }

      this.pendingMutations.push(serialized);
    }

    if (!this.batchTimer) {
      this.batchTimer = setTimeout(() => this.flushMutations(), 100);
    }
  }

  private flushMutations(): void {
    this.batchTimer = null;
    if (this.pendingMutations.length === 0) return;

    const mutations = this.pendingMutations.splice(0);

    this.emit({
      sessionId: this.sessionId(),
      timestamp: Date.now(),
      type: EventType.DomMutation,
      pageUrl: location.href,
      data: { mutations: mutations as unknown as Record<string, unknown>[] },
    });
  }

  private serializeNode(node: Node): SerializedNode {
    if (node.nodeType === 3) {
      return {
        nodeType: 3,
        textContent: node.textContent?.substring(0, 500) || "",
      };
    }

    const el = node as Element;
    const serialized: SerializedNode = {
      nodeType: 1,
      tagName: el.tagName?.toLowerCase(),
      attributes: this.getAttributes(el),
    };

    if (el.childNodes.length <= 10) {
      serialized.childNodes = Array.from(el.childNodes)
        .filter((n) => n.nodeType === 1 || n.nodeType === 3)
        .map((n) => this.serializeNode(n));
    }

    return serialized;
  }

  private getAttributes(el: Element): Record<string, string> {
    const attrs: Record<string, string> = {};
    for (const attr of Array.from(el.attributes || [])) {
      if (attr.name === "value" && shouldMask(el, this.maskAllInputs)) {
        attrs[attr.name] = maskValue(attr.value);
      } else {
        attrs[attr.name] = attr.value;
      }
    }
    return attrs;
  }

  private getNodeDescriptor(el: Element | Node): string {
    if (el.nodeType === 3) return "#text";
    const element = el as Element;
    if (!element.tagName) return "#unknown";
    const tag = element.tagName.toLowerCase();
    const id = element.id ? `#${element.id}` : "";
    return `${tag}${id}`;
  }

  private getSelector(el: Element | Node): string {
    if (el.nodeType !== 1) return "";
    const element = el as Element;
    if (element.id) return `#${element.id}`;
    return element.tagName?.toLowerCase() || "";
  }

  private observeResize(): void {
    let lastWidth = window.innerWidth;
    let lastHeight = window.innerHeight;

    const handler = () => {
      if (
        window.innerWidth !== lastWidth ||
        window.innerHeight !== lastHeight
      ) {
        lastWidth = window.innerWidth;
        lastHeight = window.innerHeight;
        this.emit({
          sessionId: this.sessionId(),
          timestamp: Date.now(),
          type: EventType.ViewportResize,
          pageUrl: location.href,
          data: { width: lastWidth, height: lastHeight },
        });
      }
    };

    window.addEventListener("resize", handler, { passive: true });
  }
}
