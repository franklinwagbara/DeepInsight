import { EventEmitter } from './MouseTracker';
export declare class FrustrationDetector {
    private emit;
    private sessionId;
    private recentClicks;
    private clickHandler;
    constructor(emit: EventEmitter, sessionId: () => string);
    start(): void;
    stop(): void;
    private onClickCapture;
    private detectRageClick;
    private detectDeadClick;
    private pruneOldClicks;
    private getDescriptor;
}
