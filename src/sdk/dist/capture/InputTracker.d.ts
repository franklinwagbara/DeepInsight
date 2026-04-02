import { EventEmitter } from './MouseTracker';
export declare class InputTracker {
    private emit;
    private sessionId;
    private maskAllInputs;
    private inputHandler;
    private changeHandler;
    constructor(emit: EventEmitter, sessionId: () => string, maskAllInputs: boolean);
    start(): void;
    stop(): void;
    private onInput;
    private getDescriptor;
    private getSelector;
}
