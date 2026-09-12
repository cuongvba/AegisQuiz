import { useEffect, useMemo, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { ChevronLeft, ChevronRight, Clock3, Flag, Lightbulb, ChevronDown, ChevronUp, RefreshCw, Home, ArrowLeft, CheckCircle, AlertTriangle } from 'lucide-react';
import confetti from 'canvas-confetti';
import { PracticeSetup } from '@/components/quiz/PracticeSetup';
import { QuestionCard } from '@/components/quiz/QuestionCard';
import { QuestionNavigator } from '@/components/quiz/QuestionNavigator';
import { ResultScreen } from '@/components/quiz/ResultScreen';
import { calculateQuizResult, isQuestionAnswered, scoreQuestion, processBackendQuestions } from '@/lib/quiz-helpers';
import { useLearnerI18n } from '@/lib/i18n';
import { learnerQuizService } from '@/services/learner-quiz.service';
import type { LearnerAnswer, LearnerQuestion, PracticeConfig } from '@/types/quiz';

const AUTH_HUB_URL = (import.meta as any).env?.VITE_AUTH_HUB_URL || window.location.origin;
const AUTH_LOGIN_PATH = `${AUTH_HUB_URL}/login`;

type LearnerUser = {
  id?: string;
  name?: string;
  email?: string;
  avatar?: string;
  role?: string;
  ou?: string;
};

function buildAuthLoginUrl(returnTo: string, lang: string): string {
  const target = new URL(AUTH_LOGIN_PATH);
  target.searchParams.set('returnTo', returnTo);
  target.searchParams.set('lang', lang);
  return target.toString();
}

function readUserFromStorage(): LearnerUser | null {
  const raw = localStorage.getItem('user');
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as LearnerUser;
    return parsed && typeof parsed === 'object' ? parsed : null;
  } catch {
    return null;
  }
}

function getUserInitial(user: LearnerUser | null): string {
  const label = (user?.name || user?.email || '').trim();
  return label ? label[0].toUpperCase() : '?';
}

function formatCountdown(totalSeconds: number): string {
  const sec = Math.max(0, totalSeconds);
  const minutes = Math.floor(sec / 60);
  const remain = sec % 60;
  return `${minutes.toString().padStart(2, '0')}:${remain.toString().padStart(2, '0')}`;
}

function playKidSound(type: 'correct' | 'wrong' | 'timeout') {
  try {
    const AudioCtx = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
    const ctx = new AudioCtx();

    const note = (freq: number, start: number, dur: number, wave: OscillatorType, vol = 0.28) => {
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      // Tiny echo for warmth
      const delay = ctx.createDelay(0.15);
      const echoGain = ctx.createGain();
      delay.delayTime.value = 0.06;
      echoGain.gain.value = 0.18;
      osc.connect(gain);
      gain.connect(ctx.destination);
      gain.connect(delay);
      delay.connect(echoGain);
      echoGain.connect(ctx.destination);
      osc.type = wave;
      osc.frequency.value = freq;
      const t0 = ctx.currentTime + start;
      gain.gain.setValueAtTime(0, t0);
      gain.gain.linearRampToValueAtTime(vol, t0 + 0.018);
      gain.gain.setValueAtTime(vol, t0 + dur - 0.03);
      gain.gain.exponentialRampToValueAtTime(0.001, t0 + dur + 0.07);
      osc.start(t0);
      osc.stop(t0 + dur + 0.12);
    };

    if (type === 'correct') {
      // === 🎺 Triumphant rising fanfare ===
      // Melody: C5-E5-G5-B5 rapid arpeggio then big C6 finish
      note(523.25, 0.00, 0.11, 'square', 0.22);   // C5
      note(659.25, 0.10, 0.11, 'square', 0.22);   // E5
      note(783.99, 0.20, 0.11, 'square', 0.22);   // G5
      note(987.77, 0.30, 0.13, 'square', 0.22);   // B5
      note(1046.50, 0.43, 0.45, 'triangle', 0.30); // C6 — big held note
      // Harmony chord under the finish
      note(261.63, 0.43, 0.42, 'sine', 0.17);     // C4
      note(392.00, 0.43, 0.42, 'sine', 0.13);     // G4
      note(523.25, 0.43, 0.42, 'sine', 0.10);     // C5 low layer
      // ✨ Sparkle tinkle on top
      note(2093, 0.44, 0.07, 'sine', 0.10);
      note(2637, 0.53, 0.06, 'sine', 0.08);
      note(3136, 0.60, 0.05, 'sine', 0.06);

    } else if (type === 'wrong') {
      // === � Gentle "uh-oh" — two soft descending piano-like notes ===
      // First note: warm round drop
      note(440, 0.00, 0.28, 'sine', 0.26);   // A4
      note(349.23, 0.22, 0.35, 'sine', 0.22); // F4
      // Soft harmonic layer for roundness
      note(220, 0.00, 0.28, 'triangle', 0.10); // A3 low
      note(174.61, 0.22, 0.35, 'triangle', 0.08); // F3 low
      // Tiny high chime accent at start so it feels musical not harsh
      note(880, 0.00, 0.05, 'sine', 0.07);   // A5 sparkle

    } else {
      // === ⏰ Clock-tick then soft bell ===
      // Two sharp ticks
      note(1320, 0.00, 0.055, 'square', 0.2);
      note(1100, 0.14, 0.055, 'square', 0.18);
      // Descending soft bell
      note(440, 0.32, 0.5, 'sine', 0.26);
      note(330, 0.38, 0.5, 'triangle', 0.18);
      note(220, 0.46, 0.6, 'sine', 0.14);
    }
  } catch { /* audio blocked — browser policy */ }
}

export function PracticePage() {
  const { lang, setLang, t } = useLearnerI18n();
  const navigate = useNavigate();

  const restart = () => {
    setAnswers({});
    setCurrentIndex(0);
    setSubmitted(false);
    setHasStarted(false);
    setSessionQuestions([]);
    setSessionConfig(null);
    setSecondsLeft(0);
    setQuestionSecondsLeft({});
    setExpiredQuestions({});
    setFlaggedQuestions({});
    setIsGlobalHintActive(false);
    setKidStreak(0);
    setKidBestStreak(0);
    setKidFeedback('');
    setKidMilestone('');
  };

  const handleRetestMistakes = (wrongQuestions: LearnerQuestion[]) => {
    if (!wrongQuestions || wrongQuestions.length === 0) return;
    setSessionQuestions(wrongQuestions);
    setAnswers({});
    setCurrentIndex(0);
    setSubmitted(false);
    setHasStarted(true);
    setSecondsLeft(wrongQuestions.length * 90);
    setKidStreak(0);
    setKidFeedback(lang === 'vi' ? '🔥 Bắt đầu phiên Chinh Phục Lỗi Sai! Cố lên bạn ơi!' : '🔥 Starting Mistake Retest! You got this!');
  };

  const handleGoBack = () => {
    if (hasStarted) {
      if (isWorkingSession) {
        const confirmExit = window.confirm(
          lang === 'vi'
            ? 'Bạn có chắc chắn muốn thoát khỏi phòng luyện thi và quay lại màn hình thiết lập? Tiến trình làm bài chưa nộp sẽ bị mất.'
            : 'Are you sure you want to exit the practice session and return to the setup screen? Your unsubmitted progress will be lost.'
        );
        if (!confirmExit) return;
      }
      restart();
    } else {
      navigate(-1);
    }
  };

  const handleBackToDashboard = () => {
    if (isWorkingSession) {
      const confirmExit = window.confirm(
        lang === 'vi'
          ? 'Bạn có chắc chắn muốn thoát và quay lại Bảng điều khiển? Tiến trình làm bài chưa nộp sẽ bị mất.'
          : 'Are you sure you want to exit and return to the Dashboard? Your unsubmitted progress will be lost.'
      );
      if (!confirmExit) return;
    }
    navigate('/');
  };

  const authLoginUrl = useMemo(() => buildAuthLoginUrl(window.location.href, lang), [lang]);
  const isGuest = !localStorage.getItem('token');
  const [currentUser, setCurrentUser] = useState<LearnerUser | null>(() => readUserFromStorage());
  const [isProfileOpen, setIsProfileOpen] = useState(false);
  const profileRef = useRef<HTMLDivElement | null>(null);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [answers, setAnswers] = useState<Record<string, LearnerAnswer>>({});
  const [submitted, setSubmitted] = useState(false);
  const [hasStarted, setHasStarted] = useState(false);
  const [sessionQuestions, setSessionQuestions] = useState<LearnerQuestion[]>([]);
  const [sessionConfig, setSessionConfig] = useState<PracticeConfig | null>(null);
  const [secondsLeft, setSecondsLeft] = useState(0);
  const [questionSecondsLeft, setQuestionSecondsLeft] = useState<Record<string, number>>({});
  const [expiredQuestions, setExpiredQuestions] = useState<Record<string, boolean>>({});
  const [flaggedQuestions, setFlaggedQuestions] = useState<Record<string, boolean>>({});
  const [isGlobalHintActive, setIsGlobalHintActive] = useState(false);
  const [isPaperNavCollapsed, setIsPaperNavCollapsed] = useState(false);
  const [kidStreak, setKidStreak] = useState(0);
  const [kidBestStreak, setKidBestStreak] = useState(0);
  const [kidFeedback, setKidFeedback] = useState('');
  const [kidMilestone, setKidMilestone] = useState('');
  const kidFeedbackTimerRef = useRef<number | null>(null);
  const kidMilestoneTimerRef = useRef<number | null>(null);

  const kidCorrectMsgs = lang === 'vi'
    ? ['🎉 Xuất sắc! Đúng rồi bạn ơi!', '⭐ Tuyệt vời! Bạn thật thông minh!', '🏆 Chính xác! Bạn quá giỏi!', '🌟 Hay lắm! Tiếp tục phát huy!', '👏 Bravo! Bạn đang làm rất tốt!', '💯 Hoàn hảo! Thông minh ghê!', '🦁 Ngầu lắm! Đúng hoàn toàn!', '🚀 Giỏi ghê! Bay lên nào!']
    : ['🎉 Great job! You rock!', '⭐ Superstar! Correct!', '🏆 Nailed it! Amazing!', '🌟 Brilliant! Keep it up!', '👏 Awesome! You are on fire!', '💯 Perfect! So smart!', '🦁 Roar! That was awesome!', '🚀 Blast off! Correct!'];

  const kidWrongMsgs = lang === 'vi'
    ? ['😊 Gần đúng rồi! Đừng bỏ cuộc nhé!', '💪 Bạn làm được! Câu tiếp thôi!', '🌈 Không sao! Tiếp tục nào bạn ơi!', '🐢 Chậm mà chắc nhé!', '🌱 Cố lên! Bạn đang tiến bộ đó!', '🤗 Không sao! Học từ sai lầm mà!', '💫 Sắp đúng rồi! Bạn đang giỏi lên!', '🦋 Mỗi lần sai là một lần học hỏi!']
    : ['😊 Almost there! Don\'t give up!', '💪 You\'ve got this! Keep going!', '🌈 That\'s okay! Try the next one!', '🐢 Slow and steady wins the race!', '🌱 Keep trying! You\'re improving!', '🤗 Mistakes help us learn!', '💫 Not yet, but you\'re getting better!', '🦋 Every mistake is a step forward!'];

  const pickKidMsg = (correct: boolean) => {
    const arr = correct ? kidCorrectMsgs : kidWrongMsgs;
    return arr[Math.floor(Math.random() * arr.length)];
  };

  const handleSignOut = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    localStorage.removeItem('dehoc-auth-return-to');
    setIsProfileOpen(false);
    window.location.assign(authLoginUrl);
  };

  const { data: topicsList = [] } = useQuery({
    queryKey: ['learner-quiz-topics-list'],
    queryFn: () => learnerQuizService.getTopics()
  });

  const { data: topicCounts = [] } = useQuery({
    queryKey: ['learner-quiz-topic-counts'],
    queryFn: () => learnerQuizService.getTopicCounts()
  });

  const { data: domainGroups = [] } = useQuery({
    queryKey: ['learner-quiz-topic-tree'],
    queryFn: () => learnerQuizService.getTopicTree()
  });

  const getTopicFullPath = (topic: any, allTopics: any[]): string => {
    const path: string[] = [topic.name];
    let parentId = topic.parentId;
    const visited = new Set<string>([topic.id]);

    while (parentId) {
      if (visited.has(parentId)) break;
      visited.add(parentId);

      const parent = allTopics.find(t => t.id === parentId);
      if (!parent) break;

      path.unshift(parent.name);
      parentId = parent.parentId;
    }

    return path.join(' → ');
  };

  const topics = useMemo(() => {
    const countMap = new Map<string, number>();
    for (const item of topicCounts) {
      const code = item.topicCode || item.TopicCode;
      const count = item.count !== undefined ? item.count : item.Count;
      if (code) {
        countMap.set(code.toUpperCase(), count);
      }
    }

    return topicsList.map((t: any) => {
      const qCount = countMap.get(t.code.toUpperCase()) || 0;
      return {
        key: t.code,
        code: t.code,
        name: getTopicFullPath(t, topicsList),
        questionCount: qCount
      };
    });
  }, [topicsList, topicCounts]);

  const [isGenerating, setIsGenerating] = useState(false);

  useEffect(() => {
    if (sessionQuestions.length === 0 || !sessionConfig) return;

    const sum = sessionQuestions.reduce((acc, item) => acc + Math.max(30, item.durationSec || 0), 0);
    const initial = sessionConfig.mode === 'exam' && sessionConfig.timeLimitSec > 0 ? sessionConfig.timeLimitSec : sum;
    setSecondsLeft(initial);
  }, [sessionQuestions, sessionConfig]);

  useEffect(() => {
    if (submitted || secondsLeft <= 0) return;

    const timer = window.setInterval(() => {
      setSecondsLeft((prev) => {
        if (prev <= 1) {
          window.clearInterval(timer);
          setSubmitted(true);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => window.clearInterval(timer);
  }, [submitted, secondsLeft]);

  const answeredCount = useMemo(() => {
    return sessionQuestions.filter((q) => isQuestionAnswered(q, answers[q.id])).length;
  }, [sessionQuestions, answers]);
  const result = calculateQuizResult(sessionQuestions, answers);
  const currentQuestion = sessionQuestions[currentIndex];
  const isWorkingSession = hasStarted && !submitted;
  const isKidMode = sessionConfig?.mode === 'kids';
  const isStudyMode = sessionConfig?.mode === 'study';
  const isPaperMode = sessionConfig?.mode === 'paper';
  const flaggedCount = useMemo(
    () => (isKidMode ? 0 : sessionQuestions.filter((q) => flaggedQuestions[q.id]).length),
    [flaggedQuestions, isKidMode, sessionQuestions],
  );

  useEffect(() => {
    if (isGuest) {
      setCurrentUser(null);
      return;
    }
    setCurrentUser(readUserFromStorage());

    const onStorage = (event: StorageEvent) => {
      if (event.key === 'user') {
        setCurrentUser(readUserFromStorage());
      }
    };

    window.addEventListener('storage', onStorage);
    return () => window.removeEventListener('storage', onStorage);
  }, [isGuest]);

  useEffect(() => {
    if (!isProfileOpen) return;

    const handleClickOutside = (event: MouseEvent) => {
      if (!profileRef.current) return;
      if (!profileRef.current.contains(event.target as Node)) {
        setIsProfileOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isProfileOpen]);

  useEffect(() => {
    if (!hasStarted || submitted || !currentQuestion) return;
    if (currentQuestion.durationSec <= 0) return;
    if (expiredQuestions[currentQuestion.id]) return;

    const timer = window.setInterval(() => {
      setQuestionSecondsLeft((prev) => {
        const current = prev[currentQuestion.id] ?? Math.max(1, currentQuestion.durationSec || 1);
        if (current <= 1) {
          window.clearInterval(timer);
          setExpiredQuestions((old) => ({ ...old, [currentQuestion.id]: true }));

          if (sessionConfig?.mode === 'kids') {
            setKidFeedback(t('kidTimeout'));
            playKidSound('timeout');
          }

          if (sessionConfig?.autoNextOnTimeout ?? true) {
            setCurrentIndex((idx) => {
              if (idx >= sessionQuestions.length - 1) {
                setSubmitted(true);
                return idx;
              }
              return idx + 1;
            });
          }

          return { ...prev, [currentQuestion.id]: 0 };
        }

        return { ...prev, [currentQuestion.id]: current - 1 };
      });
    }, 1000);

    return () => window.clearInterval(timer);
  }, [currentQuestion, expiredQuestions, hasStarted, sessionConfig?.autoNextOnTimeout, sessionConfig?.mode, sessionQuestions.length, submitted, t]);

  const updateAnswer = (questionId: string, answer: LearnerAnswer) => {
    if (expiredQuestions[questionId]) return;

    setAnswers((prev) => ({ ...prev, [questionId]: answer }));

    if (sessionConfig?.mode !== 'kids') return;

    const question = sessionQuestions.find((q) => q.id === questionId);
    if (!question || question.questionType === 'ESSAY') return;
    if (!isQuestionAnswered(question, answer)) return;

    const earned = scoreQuestion(question, answer);
    const isCorrect = earned > 0;

    // Auto-dismiss feedback + auto-next after 2 seconds
    if (kidFeedbackTimerRef.current) window.clearTimeout(kidFeedbackTimerRef.current);
    playKidSound(isCorrect ? 'correct' : 'wrong');
    setKidFeedback(pickKidMsg(isCorrect));
    kidFeedbackTimerRef.current = window.setTimeout(() => {
      setKidFeedback('');
      setCurrentIndex((idx) => {
        if (idx < sessionQuestions.length - 1) {
          return idx + 1;
        }
        // Last question → auto-submit
        setSubmitted(true);
        return idx;
      });
    }, 2000);

    setKidStreak((prev) => {
      const next = isCorrect ? prev + 1 : 0;
      setKidBestStreak((best) => Math.max(best, next));

      if (isCorrect) {
        // Confetti on correct answer
        confetti({ particleCount: 60, spread: 70, origin: { y: 0.7 }, colors: ['#06b6d4', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6'] });

        // Milestone banners
        const milestoneKey = next === 3 ? 'kidMilestone3' : next === 5 ? 'kidMilestone5' : next === 10 ? 'kidMilestone10' : null;
        if (milestoneKey) {
          if (next >= 5) {
            confetti({ particleCount: 150, spread: 100, origin: { y: 0.5 }, colors: ['#fbbf24', '#f59e0b', '#fcd34d'] });
          }
          if (kidMilestoneTimerRef.current) window.clearTimeout(kidMilestoneTimerRef.current);
          setKidMilestone(t(milestoneKey));
          kidMilestoneTimerRef.current = window.setTimeout(() => setKidMilestone(''), 3000);
        }
      }

      return next;
    });
  };

  const [showSubmitModal, setShowSubmitModal] = useState(false);

  const handleSubmitAttempt = () => {
    setShowSubmitModal(true);
  };

  const handleConfirmSubmit = () => {
    setShowSubmitModal(false);
    setSubmitted(true);
  };



  const handleStartSession = async (config: PracticeConfig) => {
    setIsGenerating(true);
    try {
      const generated = await learnerQuizService.generatePracticeQuestions(config);
      if (generated.length === 0) {
        window.alert(t('noQuestionForSetup'));
        return;
      }

      const processedQuestions = processBackendQuestions(generated, config);

      setSessionConfig(config);
      setSessionQuestions(processedQuestions);
      setHasStarted(true);
      setSubmitted(false);
      setAnswers({});
      setCurrentIndex(0);
      setKidStreak(0);
      setKidBestStreak(0);
      setKidFeedback('');
      setExpiredQuestions({});
      setFlaggedQuestions({});
      setIsGlobalHintActive(false);

      const perQuestion: Record<string, number> = {};
      for (const q of generated) {
        if (q.durationSec > 0) {
          perQuestion[q.id] = Math.max(1, q.durationSec);
        }
      }
      setQuestionSecondsLeft(perQuestion);
    } finally {
      setIsGenerating(false);
    }
  };

  return (
    <main className={`relative min-h-screen overflow-clip bg-[radial-gradient(circle_at_top_left,#a7f3d0_0%,transparent_36%),radial-gradient(circle_at_top_right,#bae6fd_0%,transparent_38%),linear-gradient(180deg,#f8fafc_0%,#e2e8f0_100%)] px-3 ${isWorkingSession ? 'py-3 md:px-4 md:py-4' : 'py-6 md:px-6 md:py-10'}`}>
      {!isWorkingSession && <div className="pointer-events-none absolute -left-10 top-20 h-52 w-52 rounded-full bg-teal-300/30 blur-3xl" />}
      {!isWorkingSession && <div className="pointer-events-none absolute -right-10 bottom-20 h-60 w-60 rounded-full bg-sky-300/30 blur-3xl" />}

      <div className={`relative mx-auto ${isWorkingSession ? 'max-w-[1500px] space-y-3' : 'max-w-[1320px] space-y-5'}`}>
        {isGuest && !isWorkingSession && (
          <section className="rounded-2xl border border-amber-200 bg-amber-50/90 px-4 py-3 text-amber-900 shadow-sm">
            <div className="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
              <div>
                <p className="text-sm font-bold">{t('guestNoticeTitle')}</p>
                <p className="text-sm text-amber-800">{t('guestNoticeBody')}</p>
              </div>
              <a
                href={authLoginUrl}
                className="inline-flex items-center justify-center rounded-xl border border-amber-300 bg-white px-4 py-2 text-sm font-semibold text-amber-700 shadow-sm transition hover:bg-amber-100"
              >
                {t('guestNoticeLogin')}
              </a>
            </div>
          </section>
        )}
        <header className="z-50 sticky top-2 p-2 md:top-4 md:p-2.5 rounded-[2rem] border border-white/70 bg-white/85 shadow-[0_24px_70px_-45px_rgba(14,116,144,0.6)] backdrop-blur">
          <div className="flex flex-row items-center justify-between gap-2">
            <div className="min-w-0 flex-1 ml-2 flex items-center gap-1.5">
              <button
                type="button"
                onClick={handleGoBack}
                className="p-1.5 text-slate-500 hover:text-cyan-600 hover:bg-slate-100 rounded-xl transition-colors cursor-pointer flex items-center justify-center border border-slate-200 bg-white/90 shadow-sm"
                title={lang === 'vi' ? 'Quay lại' : 'Back'}
              >
                <ArrowLeft className="h-4 w-4" />
              </button>
              <button
                type="button"
                onClick={handleBackToDashboard}
                className="p-1.5 text-slate-500 hover:text-cyan-600 hover:bg-slate-100 rounded-xl transition-colors cursor-pointer mr-1.5 flex items-center justify-center border border-slate-200 bg-white/90 shadow-sm"
                title={t('Dashboard')}
              >
                <Home className="h-4 w-4" />
              </button>
              <h1 className="font-black tracking-tight text-slate-900 truncate text-base leading-tight md:text-xl">
                {!isWorkingSession && <span className="hidden sm:inline-block mr-2 uppercase tracking-[0.1em] text-cyan-600 text-xs font-bold">{t('learnerChallenge')}</span>}
                {t('adaptiveAssessment')}
              </h1>
            </div>

            <div className="flex items-center justify-end gap-1.5 flex-auto">
              {isWorkingSession ? (
                <div className="inline-flex h-8 items-center gap-1.5 rounded-xl border border-cyan-200 bg-cyan-50 px-2 text-xs font-bold text-cyan-700">
                  <Clock3 className="h-3.5 w-3.5" />
                  <span>{formatCountdown(secondsLeft)}</span>
                  {sessionQuestions.length > 0 && <span className="text-cyan-800">{answeredCount}/{sessionQuestions.length}</span>}
                </div>
              ) : (
                <div className="inline-flex h-8 items-center gap-1.5 rounded-xl border border-cyan-200 bg-cyan-50 px-3 text-xs font-bold text-cyan-700 w-auto">
                  <div className="inline-flex items-center gap-2">
                    <Clock3 className="h-4 w-4" />
                    <span className="hidden sm:inline">{t('totalTime')} </span>
                    <span>{formatCountdown(secondsLeft)}</span>
                  </div>
                </div>
              )}

              {isWorkingSession && (
                <button
                  type="button"
                  onClick={handleSubmitAttempt}
                  className="inline-flex h-8 items-center justify-center gap-1 rounded-xl bg-gradient-to-r from-rose-500 via-orange-500 to-amber-500 px-2.5 text-xs font-bold text-white shadow shadow-orange-400/30 hover:brightness-105 active:scale-95 transition cursor-pointer"
                  title="Nộp bài và kết thúc làm bài sớm"
                  aria-label={t('submitAttempt')}
                >
                  <Flag className="h-3.5 w-3.5" />
                  <span className="hidden sm:inline">Nộp bài</span>
                </button>
              )}

              <select
                value={lang}
                onChange={(event) => setLang(event.target.value as 'vi' | 'en')}
                className="rounded-xl border border-slate-200 bg-white px-1 text-xs font-bold text-slate-700 outline-none ring-cyan-200 focus:ring h-8 w-14"
                aria-label="Language"
              >
                <option value="vi">VI</option>
                <option value="en">EN</option>
              </select>

              {!isGuest && currentUser && (
                <div className="relative" ref={profileRef}>
                  <button
                    type="button"
                    onClick={() => setIsProfileOpen((prev) => !prev)}
                    className="flex items-center gap-2 rounded-xl border border-slate-200 bg-white text-slate-700 hover:bg-slate-50 h-8 px-2 text-[11px]"
                    aria-haspopup="menu"
                    aria-expanded={isProfileOpen}
                  >
                    <div className="flex h-6 w-6 items-center justify-center rounded-full bg-cyan-100 text-[10px] font-bold text-cyan-700">
                      {getUserInitial(currentUser)}
                    </div>
                    <span className="hidden max-w-[100px] truncate sm:block">
                      {currentUser.name || currentUser.email?.split('@')[0] || t('userUnknown')}
                    </span>
                  </button>

                  {isProfileOpen && (
                    <div
                      role="menu"
                      className="absolute right-0 z-20 mt-2 w-64 rounded-2xl border border-slate-200 bg-white p-3 text-xs text-slate-700 shadow-lg"
                    >
                      <div className="text-[11px] font-bold uppercase tracking-[0.12em] text-slate-400">
                        {t('profileTitle')}
                      </div>
                      <div className="mt-2">
                        <div className="font-semibold text-slate-900">
                          {currentUser.name || currentUser.email || t('userUnknown')}
                        </div>
                        {currentUser.email && <div className="text-[11px] text-slate-500">{currentUser.email}</div>}
                      </div>
                      {(currentUser.role || currentUser.ou) && (
                        <div className="mt-2 flex flex-wrap gap-2">
                          {currentUser.role && (
                            <span className="rounded-full border border-cyan-200 bg-cyan-50 px-2 py-0.5 text-[11px] font-semibold text-cyan-700">
                              {currentUser.role}
                            </span>
                          )}
                          {currentUser.ou && (
                            <span className="rounded-full border border-slate-200 bg-slate-50 px-2 py-0.5 text-[11px] font-semibold text-slate-600">
                              {currentUser.ou}
                            </span>
                          )}
                        </div>
                      )}
                      <div className="mt-3 border-t border-slate-100 pt-2">
                        <a
                          href={`${AUTH_HUB_URL}/dashboard`}
                          className="block rounded-lg px-2 py-2 text-[11px] font-semibold text-cyan-700 hover:bg-cyan-50"
                        >
                          {t('profileOpenPortal')}
                        </a>
                        <a
                          href={authLoginUrl}
                          className="block rounded-lg px-2 py-2 text-[11px] font-semibold text-slate-600 hover:bg-slate-100"
                        >
                          {t('profileSwitchAccount')}
                        </a>
                        <button
                          type="button"
                          onClick={handleSignOut}
                          className="mt-1 w-full rounded-lg px-2 py-2 text-left text-[11px] font-semibold text-rose-600 hover:bg-rose-50"
                        >
                          {t('profileSignOut')}
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        </header>

        {!hasStarted ? (
          isGenerating ? (
            <div className="flex justify-center items-center h-64 text-slate-500">
              <RefreshCw className="mr-2 h-5 w-5 animate-spin" />
              Đang chuẩn bị câu hỏi từ ngân hàng...
            </div>
          ) : (
            <PracticeSetup topics={topics} domainGroups={domainGroups} onStart={handleStartSession} t={t} />
          )
        ) : submitted ? (
          <ResultScreen
            result={result}
            questions={sessionQuestions}
            answers={answers}
            flaggedQuestions={isKidMode ? {} : flaggedQuestions}
            isKidMode={isKidMode}
            kidBestStreak={kidBestStreak}
            onRestart={restart}
            onRetestMistakes={handleRetestMistakes}
            t={t}
          />
        ) : (
          <div className="grid gap-4 lg:grid-cols-[240px,minmax(0,1fr)] xl:grid-cols-[260px,minmax(0,1fr)]">
            <div className="space-y-3 lg:sticky lg:top-3 lg:h-fit">
              {sessionConfig?.mode === 'kids' && (
                <section className="rounded-3xl border border-white/70 bg-white/85 p-4 shadow-[0_24px_70px_-45px_rgba(14,116,144,0.6)] backdrop-blur">
                  <div className="flex items-center justify-between text-sm font-bold text-slate-700">
                    <span className="flex items-center gap-1">
                      🔥 <span className="text-orange-500">{kidStreak}</span>
                      <span className="ml-1 text-xs font-normal text-slate-500">{t('kidStreak')}</span>
                    </span>
                    <span className="flex items-center gap-1">
                      ⭐ <span className="text-amber-500">{kidBestStreak}</span>
                      <span className="ml-1 text-xs font-normal text-slate-500">{t('kidBest')}</span>
                    </span>
                  </div>
                  {/* Streak progress bar */}
                  <div className="mt-2 h-2 w-full rounded-full bg-slate-200">
                    <div
                      className="h-2 rounded-full bg-gradient-to-r from-orange-400 via-yellow-400 to-green-400 transition-all duration-500"
                      style={{ width: `${Math.min(100, (kidStreak / 10) * 100)}%` }}
                    />
                  </div>
                  <p className="mt-1 text-[10px] text-slate-400 text-center">{kidStreak}/10 🏆</p>
                  {kidFeedback && (
                    <p className={`mt-2 rounded-lg px-2 py-1 text-center text-sm font-semibold transition-all duration-300 ${kidFeedback.includes('🎉') ? 'bg-emerald-50 text-emerald-700' : 'bg-rose-50 text-rose-700'
                      }`}>{kidFeedback}</p>
                  )}
                  {kidMilestone && (
                    <p className="mt-2 animate-bounce rounded-lg bg-amber-50 px-2 py-1 text-center text-sm font-bold text-amber-700">{kidMilestone}</p>
                  )}
                </section>
              )}

              {!isKidMode && flaggedCount > 0 && (
                <div className="rounded-2xl border border-amber-200 bg-amber-50 px-3 py-2 text-xs font-semibold text-amber-800">
                  {t('flaggedCount')}: {flaggedCount}
                </div>
              )}
            </div>

            <section className="min-w-0 space-y-3 lg:space-y-6 mb-32">
              {isPaperMode ? (
                sessionQuestions.map((q, idx) => (
                  <div key={q.id} id={`question-${idx}`} className="scroll-mt-[100px]">
                    <div className="mb-2 text-sm font-extrabold uppercase tracking-widest text-slate-500">
                      {t('questionLabel')} {idx + 1}
                    </div>
                    <QuestionCard
                      question={q}
                      answer={answers[q.id]}
                      onAnswerChange={updateAnswer}
                      disabled={Boolean(expiredQuestions[q.id])}
                      questionSecondsLeft={q.durationSec > 0 ? (questionSecondsLeft[q.id] ?? q.durationSec) : null}
                      showHint={isGlobalHintActive}
                      t={t}
                    />
                  </div>
                ))
              ) : (
                currentQuestion && (
                  <QuestionCard
                    question={currentQuestion}
                    answer={answers[currentQuestion.id]}
                    onAnswerChange={updateAnswer}
                    disabled={Boolean(expiredQuestions[currentQuestion.id])}
                    questionSecondsLeft={currentQuestion.durationSec > 0 ? (questionSecondsLeft[currentQuestion.id] ?? currentQuestion.durationSec) : null}
                    showHint={isStudyMode && isGlobalHintActive}
                    t={t}
                  />
                )
              )}

              {!isKidMode && !isPaperMode && currentQuestion && (
                <div className="sticky bottom-3 z-40 mx-auto w-full max-w-[1500px] grid grid-cols-3 items-center gap-2 rounded-2xl border border-white/80 bg-white/95 p-2.5 shadow-[0_12px_40px_-10px_rgba(0,0,0,0.15)] backdrop-blur-md transition-all">
                  <div className="justify-self-start">
                    <button
                      type="button"
                      onClick={() => setCurrentIndex((prev) => Math.max(0, prev - 1))}
                      disabled={currentIndex === 0}
                      className="inline-flex h-10 min-w-10 items-center justify-center gap-2 rounded-xl border border-slate-200 px-3 py-2 text-sm font-semibold text-slate-700 disabled:cursor-not-allowed disabled:opacity-45"
                      aria-label={t('previous')}
                    >
                      <ChevronLeft className="h-4 w-4" />
                      <span className="hidden sm:inline">{t('previous')}</span>
                    </button>
                  </div>

                  <div className="justify-self-center">
                    {currentIndex < sessionQuestions.length - 1 ? (
                      <button
                        type="button"
                        onClick={() => setCurrentIndex((prev) => Math.min(sessionQuestions.length - 1, prev + 1))}
                        className="inline-flex h-10 min-w-10 items-center justify-center gap-2 rounded-xl bg-gradient-to-r from-cyan-500 to-sky-500 px-4 py-2 text-sm font-bold text-white shadow shadow-cyan-300/40 hover:brightness-105 active:scale-95 transition cursor-pointer"
                        aria-label={t('next')}
                      >
                        <span className="hidden sm:inline">{t('next')}</span>
                        <ChevronRight className="h-4 w-4" />
                      </button>
                    ) : (
                      <button
                        type="button"
                        onClick={handleSubmitAttempt}
                        className="inline-flex h-10 items-center justify-center gap-2 rounded-xl bg-gradient-to-r from-emerald-500 via-teal-500 to-cyan-600 px-5 py-2 text-sm font-black text-white shadow-lg shadow-emerald-400/30 hover:brightness-105 active:scale-95 transition cursor-pointer"
                        aria-label={t('finish')}
                      >
                        <CheckCircle className="h-4 w-4" />
                        <span>{t('finish') || 'Hoàn thành bài thi'}</span>
                      </button>
                    )}
                  </div>

                  <div className="flex flex-wrap justify-end gap-2 justify-self-end">
                    {/* Nút Nộp bài sớm - luôn khả dụng khi làm bài */}
                    <button
                      type="button"
                      onClick={handleSubmitAttempt}
                      className="inline-flex h-10 items-center justify-center gap-1.5 rounded-xl border border-rose-200 bg-rose-50 px-3 py-1.5 text-xs font-bold text-rose-700 hover:bg-rose-100 hover:border-rose-300 active:scale-95 transition cursor-pointer shadow-2xs"
                      title="Nộp bài và kết thúc làm bài ngay bây giờ"
                    >
                      <CheckCircle className="h-4 w-4 text-rose-600" />
                      <span className="hidden sm:inline">{t('submitAttempt') || 'Nộp bài'}</span>
                      <span className="sm:hidden">Nộp</span>
                    </button>

                    {isStudyMode && (
                      <button
                        type="button"
                        onClick={() => setIsGlobalHintActive(!isGlobalHintActive)}
                        className={`inline-flex h-10 min-w-10 items-center justify-center gap-2 rounded-xl border px-3 py-1.5 text-xs font-bold transition cursor-pointer ${isGlobalHintActive
                            ? 'border-cyan-300 bg-cyan-100 text-cyan-800'
                            : 'border-slate-200 bg-white text-slate-600 hover:border-slate-300'
                          }`}
                        aria-label={t('showHint')}
                      >
                        <Lightbulb className="h-3.5 w-3.5" />
                        <span className="hidden sm:inline">{t('showHint')}</span>
                      </button>
                    )}

                    <button
                      type="button"
                      onClick={() => {
                        const qid = currentQuestion.id;
                        setFlaggedQuestions((prev) => ({ ...prev, [qid]: !prev[qid] }));
                      }}
                      className={`inline-flex h-10 min-w-10 items-center justify-center gap-2 rounded-xl border px-3 py-1.5 text-xs font-bold transition cursor-pointer ${flaggedQuestions[currentQuestion.id]
                          ? 'border-amber-300 bg-amber-100 text-amber-800'
                          : 'border-slate-200 bg-white text-slate-600 hover:border-slate-300'
                        }`}
                      aria-label={flaggedQuestions[currentQuestion.id] ? t('unflagQuestion') : t('flagQuestion')}
                    >
                      <Flag className="h-3.5 w-3.5" />
                      <span className="hidden sm:inline">{flaggedQuestions[currentQuestion.id] ? t('unflagQuestion') : t('flagQuestion')}</span>
                    </button>
                  </div>
                </div>
              )}

              {!isPaperMode && (
                <QuestionNavigator
                  questions={sessionQuestions}
                  answers={answers}
                  expiredQuestions={expiredQuestions}
                  flaggedQuestions={isKidMode ? {} : flaggedQuestions}
                  isHintGlobalActive={isStudyMode && isGlobalHintActive}
                  currentIndex={currentIndex}
                  onPick={(idx) => setCurrentIndex(idx)}
                  onSubmitAttempt={handleSubmitAttempt}
                  t={t}
                />
              )}
            </section>
          </div>
        )}
      </div>

      {isPaperMode && !submitted && (
        <div className={`fixed bottom-0 left-0 right-0 z-50 bg-white/90 backdrop-blur-md rounded-t-3xl border border-slate-200 shadow-[0_-10px_40px_-15px_rgba(14,116,144,0.3)] transition-transform duration-300 ${isPaperNavCollapsed ? 'translate-y-full border-t' : 'translate-y-0'}`}>
          <div className="absolute left-1/2 -top-10 -translate-x-1/2">
            <button
              type="button"
              onClick={() => setIsPaperNavCollapsed(!isPaperNavCollapsed)}
              className="flex h-10 w-16 items-center justify-center rounded-t-2xl bg-white/95 backdrop-blur-md border border-b-0 border-slate-200 text-slate-500 hover:text-cyan-700 shadow-[0_-5px_10px_0_rgba(14,116,144,0.1)] transition-colors"
              aria-label={isPaperNavCollapsed ? t('expand') : t('collapse')}
              title={isPaperNavCollapsed ? t('expand') : t('collapse')}
            >
              {isPaperNavCollapsed ? <ChevronUp className="h-5 w-5" /> : <ChevronDown className="h-5 w-5" />}
            </button>
          </div>

          <div className="mx-auto max-w-5xl p-4 md:p-6 flex flex-col max-h-[60vh]">
            <div className="w-full space-y-4 flex flex-col min-h-0">
              <div className={`overflow-y-auto ${isPaperNavCollapsed ? 'opacity-0 h-0 overflow-hidden py-0' : 'opacity-100 transition-opacity duration-300 min-h-0'}`}>
                <QuestionNavigator
                  questions={sessionQuestions}
                  answers={answers}
                  expiredQuestions={expiredQuestions}
                  flaggedQuestions={{}}
                  isHintGlobalActive={isGlobalHintActive}
                  currentIndex={-1}
                  onPick={(idx) => {
                    document.getElementById(`question-${idx}`)?.scrollIntoView({ behavior: 'smooth' });
                  }}
                  onSubmitAttempt={handleSubmitAttempt}
                  t={t}
                />
              </div>
              <button
                type="button"
                onClick={() => setIsGlobalHintActive(!isGlobalHintActive)}
                className={`inline-flex w-full items-center justify-center gap-2 rounded-xl border px-3 py-3 text-sm font-bold transition ${isGlobalHintActive
                    ? 'border-emerald-300 bg-emerald-100 text-emerald-800'
                    : 'border-slate-200 bg-white text-slate-600 hover:border-slate-300'
                  }`}
                aria-label={t('showHint')}
              >
                <Lightbulb className="h-4 w-4" />
                <span>{t('showHint')}</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ─── MODAL XÁC NHẬN NỘP BÀI THI CHUYÊN NGHIỆP ─── */}
      {showSubmitModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 p-4 backdrop-blur-sm animate-in fade-in duration-200">
          <div className="w-full max-w-md rounded-3xl border border-white/80 bg-white p-6 shadow-2xl ring-1 ring-slate-900/5 animate-in zoom-in-95 duration-200">
            <div className="flex items-center gap-3 border-b border-slate-100 pb-4">
              <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-amber-100 text-amber-700 shadow-sm">
                <Flag className="h-6 w-6" />
              </div>
              <div>
                <h3 className="text-lg font-black text-slate-900 tracking-tight">Xác Nhận Nộp Bài Thi</h3>
                <p className="text-xs text-slate-500">Kiểm tra tiến độ làm bài trước khi kết thúc</p>
              </div>
            </div>

            <div className="mt-4 space-y-3">
              <div className="grid grid-cols-2 gap-2 text-center">
                <div className="rounded-2xl border border-teal-200 bg-teal-50/70 p-3">
                  <div className="text-xl font-black text-teal-700">{answeredCount} / {sessionQuestions.length}</div>
                  <div className="text-[11px] font-bold text-teal-800 uppercase tracking-wide mt-0.5">Câu đã trả lời</div>
                </div>

                <div className="rounded-2xl border border-amber-200 bg-amber-50/70 p-3">
                  <div className="text-xl font-black text-amber-700">{sessionQuestions.length - answeredCount}</div>
                  <div className="text-[11px] font-bold text-amber-800 uppercase tracking-wide mt-0.5">Câu chưa làm</div>
                </div>
              </div>

              {secondsLeft > 0 && (
                <div className="rounded-xl border border-slate-200 bg-slate-50 px-3.5 py-2.5 text-xs text-slate-600 flex items-center justify-between">
                  <span className="font-semibold flex items-center gap-1.5">
                    <Clock3 className="h-4 w-4 text-cyan-600" /> Thời gian làm bài còn lại:
                  </span>
                  <span className="font-mono font-bold text-slate-900 text-sm">{formatCountdown(secondsLeft)}</span>
                </div>
              )}

              {sessionQuestions.length - answeredCount > 0 && (
                <div className="rounded-xl border border-rose-200 bg-rose-50/90 p-3 text-xs text-rose-800 flex items-start gap-2">
                  <AlertTriangle className="h-4 w-4 shrink-0 text-rose-600 mt-0.5" />
                  <div className="leading-relaxed">
                    <span className="font-bold">Lưu ý: </span>
                    Bạn vẫn còn <strong>{sessionQuestions.length - answeredCount} câu chưa trả lời</strong>. Các câu hỏi này sẽ tính là 0 điểm khi nộp bài.
                  </div>
                </div>
              )}
            </div>

            <div className="mt-6 flex items-center justify-end gap-3 border-t border-slate-100 pt-4">
              <button
                type="button"
                onClick={() => setShowSubmitModal(false)}
                className="rounded-xl border border-slate-200 px-4 py-2.5 text-xs font-bold text-slate-600 hover:bg-slate-100 transition cursor-pointer"
              >
                Tiếp tục làm bài
              </button>
              <button
                type="button"
                onClick={handleConfirmSubmit}
                className="rounded-xl bg-gradient-to-r from-rose-500 via-orange-500 to-amber-500 px-5 py-2.5 text-xs font-black text-white shadow-md shadow-orange-400/30 hover:brightness-105 active:scale-98 transition cursor-pointer"
              >
                Xác nhận nộp bài
              </button>
            </div>
          </div>
        </div>
      )}
    </main>
  );
}



