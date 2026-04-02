import { DeepInsightConfig } from "../types/events";

const STORAGE_KEY = "di_session";
const SESSION_TIMEOUT = 30 * 60 * 1000; // 30 minutes

interface SessionData {
  id: string;
  startedAt: number;
  lastActivity: number;
}

export class SessionManager {
  private session: SessionData | null = null;
  private config: DeepInsightConfig;

  constructor(config: DeepInsightConfig) {
    this.config = config;
  }

  getSessionId(): string {
    this.ensureSession();
    return this.session!.id;
  }

  touch(): void {
    if (this.session) {
      this.session.lastActivity = Date.now();
      this.persist();
    }
  }

  reset(): void {
    this.session = null;
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      // sessionStorage unavailable
    }
  }

  private ensureSession(): void {
    if (this.session && !this.isExpired()) {
      return;
    }

    const restored = this.restore();
    if (restored && !this.isExpired(restored)) {
      this.session = restored;
      return;
    }

    this.session = {
      id: this.generateId(),
      startedAt: Date.now(),
      lastActivity: Date.now(),
    };
    this.persist();
  }

  private isExpired(session?: SessionData): boolean {
    const s = session || this.session;
    if (!s) return true;
    return Date.now() - s.lastActivity > SESSION_TIMEOUT;
  }

  private persist(): void {
    if (!this.session) return;
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(this.session));
    } catch {
      // sessionStorage unavailable (private browsing, etc.)
    }
  }

  private restore(): SessionData | null {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      return JSON.parse(raw) as SessionData;
    } catch {
      return null;
    }
  }

  private generateId(): string {
    if (typeof crypto !== "undefined" && crypto.randomUUID) {
      return crypto.randomUUID();
    }
    // Fallback for older browsers
    return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
      const r = (Math.random() * 16) | 0;
      const v = c === "x" ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    });
  }

  getSampleDecision(): boolean {
    const rate = this.config.sampleRate ?? 1.0;
    if (rate >= 1.0) return true;
    if (rate <= 0.0) return false;

    const sessionId = this.getSessionId();
    // Deterministic sampling based on session ID
    let hash = 0;
    for (let i = 0; i < sessionId.length; i++) {
      const char = sessionId.charCodeAt(i);
      hash = (hash << 5) - hash + char;
      hash |= 0;
    }
    return (Math.abs(hash) % 100) / 100 < rate;
  }
}
