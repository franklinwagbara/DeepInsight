import { BaseEvent } from '../types/events';
export type EventEmitter = (event: BaseEvent) => void;
export declare class MouseTracker {
    private emit;
    private sessionId;
    private throttleMs;
    private lastMoveTime;
    private lastX;
    private lastY;
    private moveHandler;
    private clickHandler;
    constructor(emit: EventEmitter, sessionId: () => string, throttleMs?: number);
    start(): void;
    stop(): void;
    private onMouseMove;
    private onClick;
    private getElementDescriptor;
    private getSelector;
    private getElementText;
}
