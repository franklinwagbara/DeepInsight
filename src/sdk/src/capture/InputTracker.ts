import { BaseEvent, EventType } from "../types/events";
import { EventEmitter } from "./MouseTracker";
import { shouldMask, maskValue, shouldExclude } from "../privacy/Masking";

export class InputTracker {
  private emit: EventEmitter;
  private sessionId: () => string;
  private maskAllInputs: boolean;
  private inputHandler: ((e: Event) => void) | null = null;
  private changeHandler: ((e: Event) => void) | null = null;

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
    this.inputHandler = (e: Event) => this.onInput(e);
    this.changeHandler = (e: Event) => this.onInput(e);

    document.addEventListener("input", this.inputHandler, {
      passive: true,
      capture: true,
    });
    document.addEventListener("change", this.changeHandler, {
      passive: true,
      capture: true,
    });
  }

  stop(): void {
    if (this.inputHandler) {
      document.removeEventListener("input", this.inputHandler, {
        capture: true,
      });
    }
    if (this.changeHandler) {
      document.removeEventListener("change", this.changeHandler, {
        capture: true,
      });
    }
  }

  private onInput(e: Event): void {
    const target = e.target as
      | HTMLInputElement
      | HTMLTextAreaElement
      | HTMLSelectElement;
    if (!target || !target.tagName) return;
    if (shouldExclude(target)) return;

    const tag = target.tagName.toLowerCase();
    if (!["input", "textarea", "select"].includes(tag)) return;

    const masked = shouldMask(target, this.maskAllInputs);
    const value = masked ? maskValue(target.value) : target.value;

    this.emit({
      sessionId: this.sessionId(),
      timestamp: Date.now(),
      type: EventType.Input,
      pageUrl: location.href,
      data: {
        element: this.getDescriptor(target),
        selector: this.getSelector(target),
        value,
        masked,
      },
    });
  }

  private getDescriptor(el: Element): string {
    const tag = el.tagName.toLowerCase();
    const type = el.getAttribute("type") || "";
    const name = el.getAttribute("name") || "";
    return `${tag}[type=${type}][name=${name}]`;
  }

  private getSelector(el: Element): string {
    if (el.id) return `#${el.id}`;
    const name = el.getAttribute("name");
    if (name) return `${el.tagName.toLowerCase()}[name="${name}"]`;
    return el.tagName.toLowerCase();
  }
}
