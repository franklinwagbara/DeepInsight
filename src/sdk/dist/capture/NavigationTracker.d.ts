import { EventEmitter } from './MouseTracker';
export declare class NavigationTracker {
    private emit;
    private sessionId;
    private lastUrl;
    private popstateHandler;
    private originalPushState;
    private originalReplaceState;
    constructor(emit: EventEmitter, sessionId: () => string);
    start(): void;
    stop(): void;
    private onNavigation;
    private emitPageView;
}
