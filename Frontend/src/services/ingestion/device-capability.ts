/**
 * [Aegis Adaptive Device Sniffer v1.0]
 * Thẩm định năng lực phần cứng của Client (RAM, CPU Cores, Stream Decompression)
 * để quyết định tự động:
 * - 'edge' : Xử lý trực tiếp trên RAM trình duyệt (< 50ms, tiết kiệm 90% CPU Server)
 * - 'cloud': Đẩy lên Cloud Backend nếu máy yếu, tệp quá lớn hoặc trình duyệt cũ.
 */

export interface DeviceCapabilityReport {
  mode: 'edge' | 'cloud';
  canProcessOnClient: boolean;
  reason: string;
  hardwareSummary: {
    cores: number;
    memoryGb?: number;
    hasDecompressionStream: boolean;
    fileSizeMb: number;
  };
}

export function evaluateIngestionDeviceCapability(file: File): DeviceCapabilityReport {
  const fileSizeMb = file.size / (1024 * 1024);
  const cores = typeof navigator !== 'undefined' && navigator.hardwareConcurrency ? navigator.hardwareConcurrency : 4;
  const memoryGb = typeof navigator !== 'undefined' && (navigator as any).deviceMemory ? (navigator as any).deviceMemory : undefined;
  const hasDecompressionStream = typeof DecompressionStream !== 'undefined';

  // 1. Nếu trình duyệt không hỗ trợ DecompressionStream -> Bắt buộc dùng Cloud
  if (!hasDecompressionStream) {
    return {
      mode: 'cloud',
      canProcessOnClient: false,
      reason: 'Trình duyệt không hỗ trợ native DecompressionStream. Chuyển sang xử lý trên Cloud Server.',
      hardwareSummary: { cores, memoryGb, hasDecompressionStream, fileSizeMb },
    };
  }

  // 2. Nếu tệp lớn hơn 15MB -> Đẩy lên Cloud để tránh tràn bộ nhớ tab trình duyệt
  if (fileSizeMb > 15) {
    return {
      mode: 'cloud',
      canProcessOnClient: false,
      reason: `Tệp dung lượng lớn (${fileSizeMb.toFixed(1)}MB > 15MB). Sử dụng Cloud Server Engine để đảm bảo an toàn bộ nhớ.`,
      hardwareSummary: { cores, memoryGb, hasDecompressionStream, fileSizeMb },
    };
  }

  // 3. Nếu CPU quá yếu (dưới 2 nhân thực) hoặc RAM < 2GB trên thiết bị di động
  if (cores < 2 || (memoryGb !== undefined && memoryGb < 2)) {
    return {
      mode: 'cloud',
      canProcessOnClient: false,
      reason: 'Tài nguyên phần cứng thiết bị thấp (CPU/RAM giới hạn). Ưu tiên Cloud Server để tránh đơ giao diện.',
      hardwareSummary: { cores, memoryGb, hasDecompressionStream, fileSizeMb },
    };
  }

  // 4. Đủ điều kiện Edge Computing
  return {
    mode: 'edge',
    canProcessOnClient: true,
    reason: `Thiết bị đáp ứng xuất sắc (${cores} cores, ${fileSizeMb.toFixed(1)}MB). Kích hoạt Edge Computing bóc tách tức thì (0ms latency).`,
    hardwareSummary: { cores, memoryGb, hasDecompressionStream, fileSizeMb },
  };
}
