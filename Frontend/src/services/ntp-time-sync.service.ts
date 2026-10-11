/**
 * [AegisGeoNet NTP Synchronization Client Service]
 * Đo đạc độ lệch đồng hồ máy khách - máy chủ (Clock Skew) và độ trễ khứ hồi (Round-Trip Time).
 * Bảo đảm công bằng tuyệt đối cho giải đấu cướp chuông Gameshow toàn cầu.
 */

export interface NtpSyncResult {
  roundTripTimeMs: number;
  clockSkewMs: number;
  sessionSecretToken: string;
  isCalibrated: boolean;
}

class NtpTimeSyncService {
  private clockSkewMs: number = 0;
  private roundTripTimeMs: number = 0;
  private sessionSecretToken: string = '';
  private isCalibrated: boolean = false;

  /**
   * Khởi động quá trình đo đạc 3-chặng để tìm mẫu có RTT tối ưu nhất
   */
  public async calibrateAsync(sampleCount: number = 3): Promise<NtpSyncResult> {
    const samples: { rtt: number; skew: number; secret: string }[] = [];

    for (let i = 0; i < sampleCount; i++) {
      try {
        const t1 = performance.timeOrigin + performance.now(); // Epoch ms with sub-millisecond precision
        const t1Us = Math.floor(t1 * 1000);

        const response = await fetch('/api/v1/arena/time-sync/ping', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            clientSendTimestampUs: t1Us,
            clientSessionId: 'arena_' + Math.random().toString(36).substring(2, 9)
          })
        });

        if (!response.ok) continue;

        const data = await response.json();
        const t4 = performance.timeOrigin + performance.now();
        const t4Us = Math.floor(t4 * 1000);

        const t2Us = Number(data.serverReceiveTimestampUs);
        const t3Us = Number(data.serverTransmitTimestampUs);

        // Tính RTT và Skew theo microseconds -> quy đổi ra mili-giây
        const rttUs = (t4Us - t1Us) - (t3Us - t2Us);
        const skewUs = ((t2Us - t1Us) + (t3Us - t4Us)) / 2;

        samples.push({
          rtt: rttUs / 1000,
          skew: skewUs / 1000,
          secret: data.sessionSecretToken || ''
        });

        // Nghỉ 50ms giữa các lượt ping
        await new Promise((r) => setTimeout(r, 50));
      } catch (err) {
        console.warn('[NtpTimeSync] Lỗi mẫu đo đạc NTP:', err);
      }
    }

    if (samples.length > 0) {
      // Chọn mẫu có RTT nhỏ nhất để giảm thiểu nhiễu đường truyền mạng
      samples.sort((a, b) => a.rtt - b.rtt);
      const best = samples[0];
      this.roundTripTimeMs = best.rtt;
      this.clockSkewMs = best.skew;
      this.sessionSecretToken = best.secret;
      this.isCalibrated = true;
    }

    return {
      roundTripTimeMs: this.roundTripTimeMs,
      clockSkewMs: this.clockSkewMs,
      sessionSecretToken: this.sessionSecretToken,
      isCalibrated: this.isCalibrated
    };
  }

  /**
   * Lấy dấu thời gian hiện tại của ngón tay thí sinh đã được chuẩn hóa theo đồng hồ máy chủ
   */
  public getNormalizedFingerTimestampUs(): number {
    const localNowMs = performance.timeOrigin + performance.now();
    const localNowUs = Math.floor(localNowMs * 1000);
    const skewUs = Math.floor(this.clockSkewMs * 1000);
    return localNowUs + skewUs;
  }

  public getSessionSecret(): string {
    return this.sessionSecretToken;
  }

  public getSkewMs(): number {
    return this.clockSkewMs;
  }

  public getRttMs(): number {
    return this.roundTripTimeMs;
  }
}

export const ntpTimeSyncService = new NtpTimeSyncService();
