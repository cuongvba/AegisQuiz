import api from '@/services/api';
import type { ArenaGameCode, ArenaRoomState } from '@/types/arena';

// ─── Web Audio Synthesizer Cho Đấu Trường Gameshow ──────────────────────────
export function playArenaSfx(type: 'buzzer' | 'wheel_tick' | 'victory' | 'drop' | 'clock') {
  try {
    const AudioCtx = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
    if (!AudioCtx) return;
    const ctx = new AudioCtx();
    const now = ctx.currentTime;
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.connect(gain);
    gain.connect(ctx.destination);

    if (type === 'buzzer') {
      // Tiếng chuông bấm giật quyền trả lời vang dội
      osc.type = 'square';
      osc.frequency.setValueAtTime(880, now);
      osc.frequency.setValueAtTime(1760, now + 0.08);
      gain.gain.setValueAtTime(0.3, now);
      gain.gain.exponentialRampToValueAtTime(0.001, now + 0.35);
      osc.start(now);
      osc.stop(now + 0.35);
    } else if (type === 'wheel_tick') {
      // Tiếng gõ lách cách nan nón
      osc.type = 'triangle';
      osc.frequency.setValueAtTime(1200, now);
      gain.gain.setValueAtTime(0.15, now);
      gain.gain.exponentialRampToValueAtTime(0.001, now + 0.04);
      osc.start(now);
      osc.stop(now + 0.04);
    } else if (type === 'victory') {
      // Tiếng chuông vàng chiến thắng
      osc.type = 'sine';
      osc.frequency.setValueAtTime(523.25, now); // C5
      osc.frequency.setValueAtTime(659.25, now + 0.1); // E5
      osc.frequency.setValueAtTime(783.99, now + 0.2); // G5
      osc.frequency.setValueAtTime(1046.50, now + 0.3); // C6
      gain.gain.setValueAtTime(0.25, now);
      gain.gain.exponentialRampToValueAtTime(0.001, now + 0.8);
      osc.start(now);
      osc.stop(now + 0.8);
    } else if (type === 'drop') {
      // Tiếng tụt dốc Nhanh như chớp
      osc.type = 'sawtooth';
      osc.frequency.setValueAtTime(440, now);
      osc.frequency.exponentialRampToValueAtTime(110, now + 0.4);
      gain.gain.setValueAtTime(0.25, now);
      gain.gain.exponentialRampToValueAtTime(0.001, now + 0.42);
      osc.start(now);
      osc.stop(now + 0.42);
    } else if (type === 'clock') {
      // Đếm ngược tích tắc
      osc.type = 'sine';
      osc.frequency.setValueAtTime(1000, now);
      gain.gain.setValueAtTime(0.1, now);
      gain.gain.exponentialRampToValueAtTime(0.001, now + 0.05);
      osc.start(now);
      osc.stop(now + 0.05);
    }
  } catch {
    /* Safe ignore browser audio policy */
  }
}

export const arenaService = {
  async getGames() {
    try {
      const res = await api.get('/api/arena/games');
      return res.data;
    } catch {
      return [];
    }
  },

  async getRooms(gameCode?: string) {
    try {
      const res = await api.get('/api/arena/rooms', { params: { gameCode } });
      return res.data;
    } catch {
      return [];
    }
  },

  async createRoom(gameCode: ArenaGameCode, roomName: string, hostName?: string): Promise<ArenaRoomState> {
    const res = await api.post('/api/arena/rooms', {
      gameCode,
      roomName,
      hostUserId: `player-${Math.floor(Math.random() * 10000)}`,
      hostName: hostName || 'Kiện Tướng Tri Thức',
    });
    return res.data;
  },

  async getRoom(id: string): Promise<ArenaRoomState | null> {
    try {
      const res = await api.get(`/api/arena/rooms/${id}`);
      return res.data;
    } catch {
      return null;
    }
  },

  async getTemplates(): Promise<any[]> {
    try {
      const res = await api.get('/api/arena/templates');
      return res.data;
    } catch {
      return [];
    }
  },

  async getTemplate(gameCode: string): Promise<any | null> {
    try {
      const res = await api.get(`/api/arena/templates/${gameCode}`);
      return res.data;
    } catch {
      return null;
    }
  },

  async saveTemplate(manifest: any): Promise<any> {
    try {
      const res = await api.post('/api/arena/templates', manifest);
      return res.data;
    } catch (err: any) {
      throw new Error(err.response?.data?.message || 'Không thể lưu template gameshow');
    }
  },

  async deleteTemplate(gameCode: string): Promise<any> {
    try {
      const res = await api.delete(`/api/arena/templates/${gameCode}`);
      return res.data;
    } catch (err: any) {
      throw new Error(err.response?.data?.message || 'Không thể xóa template');
    }
  },
};

