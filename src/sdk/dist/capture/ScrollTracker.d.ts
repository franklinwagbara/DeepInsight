import { EventEmitter } from './MouseTracker';
export declare class ScrollTracker {
    private emit;
    private sessionId;
    private throttleMs;
    private lastEmitTime;
    private maxDepthPct;
    private handler;
    constructor(emit: EventEmitter, sessionId: () => string);
    start(): void;
    stop(): void;
    private onScroll;
    getMaxDepth(): number;
}
