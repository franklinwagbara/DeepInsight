import { EventEmitter } from './MouseTracker';
export declare class DomObserver {
    private emit;
    private sessionId;
    private maskAllInputs;
    private observer;
    private batchTimer;
    private pendingMutations;
    constructor(emit: EventEmitter, sessionId: () => string, maskAllInputs: boolean);
    start(): void;
    stop(): void;
    captureSnapshot(): void;
    private sanitizeNode;
    private processMutations;
    private flushMutations;
    private serializeNode;
    private getAttributes;
    private getNodeDescriptor;
    private getSelector;
    private observeResize;
}
