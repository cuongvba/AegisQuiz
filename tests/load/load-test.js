/**
 * AegisQuiz — k6 Load Test Suite
 * =================================
 * Mục tiêu Phase 0: 500 CCU, P99 < 500ms
 * Mục tiêu Phase 1: 5,000 CCU, P99 < 300ms
 *
 * Chạy: k6 run load-test.js -e BASE_URL=http://localhost:8080 -e TOKEN=your_jwt
 */

import http from 'k6/http';
import ws   from 'k6/ws';
import { check, group, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';
import { randomItem, randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

// ── Custom Metrics ─────────────────────────────────────────────────────────
const errorRate         = new Rate('error_rate');
const attemptLatency    = new Trend('attempt_submit_latency', true);
const aiExplainLatency  = new Trend('ai_explain_latency', true);
const totalAttempts     = new Counter('total_attempts');

// ── Environment ────────────────────────────────────────────────────────────
const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const TOKEN    = __ENV.TOKEN    || '';

const HEADERS = {
  'Content-Type' : 'application/json',
  'Authorization': TOKEN ? `Bearer ${TOKEN}` : '',
};

// ── Test Configuration ─────────────────────────────────────────────────────
export const options = {
  scenarios: {
    // Scenario 1: Ramp up đến 500 CCU — kiểm tra khả năng chịu tải
    ramp_to_500:  {
      executor         : 'ramping-vus',
      startVUs         : 0,
      stages           : [
        { duration: '2m', target: 100  },  // Warm up
        { duration: '3m', target: 500  },  // Ramp to target
        { duration: '5m', target: 500  },  // Sustained load
        { duration: '2m', target: 0    },  // Cool down
      ],
      gracefulRampDown : '30s',
    },

    // Scenario 2: Spike test — đột ngột 1,000 users
    spike_test: {
      executor : 'ramping-vus',
      startTime: '12m',
      stages   : [
        { duration: '30s', target: 1000 },
        { duration: '1m',  target: 1000 },
        { duration: '30s', target: 0    },
      ],
    },
  },

  thresholds: {
    // ── SLA Requirements ──────────────────────────────────────
    'http_req_duration{name:attempt_submit}': ['p(99)<500'],   // P99 < 500ms
    'http_req_duration{name:get_questions}' : ['p(99)<300'],   // P99 < 300ms
    'http_req_duration{name:ai_explain}'    : ['p(90)<5000'],  // AI: P90 < 5s
    'http_req_failed'                        : ['rate<0.01'],  // Error rate < 1%
    'error_rate'                             : ['rate<0.01'],
    'attempt_submit_latency'                 : ['p(99)<500'],
  },
};

// ── Main Virtual User Flow ─────────────────────────────────────────────────
export default function () {
  const userId = `user-${__VU}-${__ITER}`;

  group('1. Lấy danh sách câu hỏi (paged)', () => {
    const res = http.get(`${BASE_URL}/api/quiz/questions?page=1&pageSize=10`, {
      headers,
      tags   : { name: 'get_questions' },
    });

    check(res, {
      'status 200'    : (r) => r.status === 200,
      'has items'     : (r) => JSON.parse(r.body)?.items?.length > 0,
      'has total'     : (r) => JSON.parse(r.body)?.total >= 0,
    });

    errorRate.add(res.status !== 200);
    sleep(randomIntBetween(1, 2));
  });

  group('2. Nộp kết quả câu hỏi (critical path)', () => {
    const payload = JSON.stringify({
      userId          : userId,
      questionId      : '00000000-0000-0000-0000-000000000001',
      category        : 'Toán',
      difficulty      : 2,
      selectedAnswer  : '1',
      isCorrect       : Math.random() > 0.4,  // 60% đúng
      timeSpentSeconds: randomIntBetween(10, 120),
    });

    const start = Date.now();
    const res   = http.post(`${BASE_URL}/api/quiz/attempt`, payload, {
      headers,
      tags  : { name: 'attempt_submit' },
    });
    attemptLatency.add(Date.now() - start);
    totalAttempts.add(1);

    check(res, {
      'attempt saved' : (r) => r.status === 200,
      'has attemptId' : (r) => JSON.parse(r.body)?.attemptId !== undefined,
    });

    errorRate.add(res.status !== 200);
    sleep(randomIntBetween(2, 5));
  });

  group('3. Lấy leaderboard', () => {
    const res = http.get(`${BASE_URL}/api/quiz/leaderboard?period=weekly&top=10`, {
      headers,
      tags: { name: 'get_leaderboard' },
    });

    check(res, { 'leaderboard ok': (r) => r.status === 200 });
    sleep(1);
  });

  // 20% user thử AI explain
  if (Math.random() < 0.2) {
    group('4. AI Giải thích câu hỏi (expensive)', () => {
      const dummyId = '00000000-0000-0000-0000-000000000001';
      const start   = Date.now();
      const res     = http.post(
        `${BASE_URL}/api/quiz/${dummyId}/explain`,
        JSON.stringify({ selectedAnswer: '2' }),
        { headers, tags: { name: 'ai_explain' } }
      );
      aiExplainLatency.add(Date.now() - start);

      check(res, {
        'AI explain ok'     : (r) => r.status === 200 || r.status === 404,
        'has explanation'   : (r) => r.status !== 200 || JSON.parse(r.body)?.explanation?.length > 0,
      });
      sleep(2);
    });
  }
}

// ── Setup / Teardown ───────────────────────────────────────────────────────
export function handleSummary(data) {
  const errorPct = (data.metrics.error_rate?.values?.rate ?? 0) * 100;
  const p99      = data.metrics['http_req_duration']?.values?.['p(99)'] ?? 0;

  console.log('\n========================================');
  console.log('   AegisQuiz Load Test Summary');
  console.log('========================================');
  console.log(`Total Requests : ${data.metrics.http_reqs?.values?.count ?? 0}`);
  console.log(`Error Rate     : ${errorPct.toFixed(2)}%`);
  console.log(`P99 Latency    : ${p99.toFixed(0)}ms`);
  console.log(`Max VUs        : ${data.metrics.vus_max?.values?.max ?? 0}`);

  const passed = errorPct < 1 && p99 < 500;
  console.log(`\nResult: ${passed ? '✅ PASSED' : '❌ FAILED'}`);
  console.log('========================================\n');

  return {
    'stdout'          : JSON.stringify(data, null, 2),
    'load-test-result.json': JSON.stringify(data),
  };
}
