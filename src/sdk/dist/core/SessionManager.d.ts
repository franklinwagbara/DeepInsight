import { DeepInsightConfig } from '../types/events';
export declare class SessionManager {
    private session;
    private config;
    constructor(config: DeepInsightConfig);
    getSessionId(): string;
    touch(): void;
    reset(): void;
    private ensureSession;
    private isExpired;
    private persist;
    private restore;
    private generateId;
    getSampleDecision(): boolean;
}
