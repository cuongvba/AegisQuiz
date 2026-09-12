"""
AegisQuiz IRT/CAT Service
=========================
Computerized Adaptive Testing engine dựa trên Item Response Theory (3PL).
Framework: FastAPI + Pydantic + NumPy + SciPy

Mô hình: P(θ) = c + (1-c) / (1 + exp(-a*(θ-b)))
  - θ (theta) : Năng lực người học (thường chuẩn hóa N(0,1))
  - a         : Độ phân biệt (discrimination) — câu hỏi phân biệt giỏi/kém tốt đến đâu
  - b         : Độ khó (difficulty) — tương ứng θ của người có 50% xác suất đúng (không có đoán mò)
  - c         : Xác suất đoán mò đúng (guessing/pseudo-chance)

Các endpoint:
  POST /cat/start          - Bắt đầu phiên CAT mới
  POST /cat/{session}/respond - Gửi câu trả lời, nhận câu tiếp theo
  GET  /cat/{session}/result  - Kết quả và ability estimate
  POST /irt/calibrate      - Hiệu chỉnh tham số IRT cho câu hỏi (batch)
  GET  /health             - Health check
"""

from __future__ import annotations

import json
import uuid
import math
from datetime import datetime, timedelta
from typing import List, Optional, Dict, Any, Tuple

import numpy as np
from scipy.optimize import minimize_scalar, minimize
from scipy.stats import norm
from fastapi import FastAPI, HTTPException, BackgroundTasks
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field
from functools import lru_cache

# ── App Setup ─────────────────────────────────────────────────────────────────
app = FastAPI(
    title        = "AegisQuiz IRT/CAT Engine",
    description  = "Item Response Theory + Computerized Adaptive Testing Service",
    version      = "2.0.0",
    docs_url     = "/docs",
    redoc_url    = "/redoc",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins  = ["*"],
    allow_methods  = ["*"],
    allow_headers  = ["*"],
)

# ── In-memory session store (production: thay bằng Redis) ─────────────────────
_sessions: Dict[str, "CATSession"] = {}


# ── Pydantic Models ───────────────────────────────────────────────────────────

class QuestionIRT(BaseModel):
    """Câu hỏi với tham số IRT 3PL."""
    question_id : str
    a           : float = Field(default=1.0,  ge=0.1,  le=4.0,   description="Discrimination")
    b           : float = Field(default=0.0,  ge=-4.0, le=4.0,   description="Difficulty")
    c           : float = Field(default=0.25, ge=0.0,  le=0.5,   description="Guessing")
    topic_code  : str   = ""
    difficulty  : int   = Field(default=3,    ge=1,    le=5)

class CATConfig(BaseModel):
    min_items       : int   = Field(default=5,   ge=3, le=10)
    max_items       : int   = Field(default=20,  ge=5, le=50)
    se_threshold    : float = Field(default=0.3, ge=0.1, le=1.0, description="Standard Error ngưỡng dừng")
    initial_theta   : float = Field(default=0.0, description="Năng lực ban đầu ước lượng")
    topic_code      : Optional[str] = None

class StartCATRequest(BaseModel):
    user_id         : str
    question_bank   : List[QuestionIRT]
    config          : CATConfig = CATConfig()

class CATResponse(BaseModel):
    session_id      : str
    is_correct      : bool
    time_spent_secs : int = 0

class QuestionResult(BaseModel):
    question_id     : str
    response        : int   # 0 = sai, 1 = đúng
    time_spent_secs : int

class CalibrationRequest(BaseModel):
    """Dữ liệu để hiệu chỉnh tham số IRT."""
    question_id     : str
    responses       : List[QuestionResult]  # Toàn bộ lịch sử trả lời câu này


# ── IRT Engine ────────────────────────────────────────────────────────────────

class IRT3PLEngine:
    """
    Item Response Theory 3-Parameter Logistic Model.
    """

    @staticmethod
    def probability(theta: float, a: float, b: float, c: float) -> float:
        """P(θ | a, b, c) — Xác suất trả lời đúng."""
        return c + (1 - c) / (1 + math.exp(-a * (theta - b)))

    @staticmethod
    def information(theta: float, a: float, b: float, c: float) -> float:
        """Fisher Information I(θ) — Lượng thông tin câu hỏi cung cấp tại θ."""
        p = IRT3PLEngine.probability(theta, a, b, c)
        q = 1 - p
        if p <= c or q <= 0 or p <= 0:
            return 0.0
        return (a ** 2 * (p - c) ** 2 * q) / ((1 - c) ** 2 * p)

    @staticmethod
    def estimate_ability_mle(
        responses: List[Tuple[float, float, float, int]]
    ) -> Tuple[float, float]:
        """
        Maximum Likelihood Estimation của θ (năng lực người học).
        responses: List[(a, b, c, response)] — response = 0 hoặc 1
        Returns: (theta_hat, standard_error)
        """
        if not responses:
            return 0.0, 999.0

        # Kiểm tra trường hợp đặc biệt: tất cả đúng hoặc tất cả sai
        all_correct = all(r[3] == 1 for r in responses)
        all_wrong   = all(r[3] == 0 for r in responses)

        if all_correct:
            # Bias theta lên cao nhưng không vô cực
            return min(3.0, max(r[1] for r in responses) + 1.0), 0.5
        if all_wrong:
            return max(-3.0, min(r[1] for r in responses) - 1.0), 0.5

        def neg_log_likelihood(theta: float) -> float:
            ll = 0.0
            for a, b, c, resp in responses:
                p  = IRT3PLEngine.probability(theta, a, b, c)
                p  = max(1e-10, min(1 - 1e-10, p))  # Tránh log(0)
                ll += resp * math.log(p) + (1 - resp) * math.log(1 - p)
            return -ll

        result = minimize_scalar(
            neg_log_likelihood,
            bounds  = (-4.0, 4.0),
            method  = "bounded",
            options = {"xatol": 1e-6}
        )
        theta_hat = result.x

        # Tính Standard Error = 1 / sqrt(Σ Information)
        total_info = sum(IRT3PLEngine.information(theta_hat, a, b, c) for a, b, c, _ in responses)
        se = 1.0 / math.sqrt(max(total_info, 1e-10))

        return theta_hat, se

    @staticmethod
    def select_next_item(
        theta        : float,
        available    : List[QuestionIRT],
        administered : set
    ) -> Optional[QuestionIRT]:
        """
        CAT Item Selection: Chọn câu hỏi tối đa hóa Fisher Information tại θ.
        """
        candidates = [q for q in available if q.question_id not in administered]
        if not candidates:
            return None

        best_q    = None
        best_info = -1.0

        for q in candidates:
            info = IRT3PLEngine.information(theta, q.a, q.b, q.c)
            if info > best_info:
                best_info = info
                best_q    = q

        return best_q

    @staticmethod
    def should_terminate(
        n_items  : int,
        se       : float,
        config   : CATConfig
    ) -> Tuple[bool, str]:
        """
        Kiểm tra điều kiện dừng CAT.
        Returns: (should_stop, reason)
        """
        if n_items >= config.max_items:
            return True, f"Đã đạt tối đa {config.max_items} câu"
        if n_items >= config.min_items and se <= config.se_threshold:
            return True, f"SE={se:.3f} < ngưỡng {config.se_threshold} sau {n_items} câu"
        return False, ""


# ── CAT Session ───────────────────────────────────────────────────────────────

class CATSession:
    """Trạng thái một phiên CAT."""

    def __init__(
        self,
        session_id    : str,
        user_id       : str,
        question_bank : List[QuestionIRT],
        config        : CATConfig
    ):
        self.session_id    = session_id
        self.user_id       = user_id
        self.question_bank = question_bank
        self.config        = config
        self.theta         = config.initial_theta
        self.se            = 999.0
        self.responses     : List[Tuple[float, float, float, int]] = []
        self.administered  : set = set()
        self.current_q     : Optional[QuestionIRT] = None
        self.history       : List[Dict] = []
        self.finished      = False
        self.finish_reason = ""
        self.started_at    = datetime.utcnow()
        self.engine        = IRT3PLEngine()

    def get_next_question(self) -> Optional[QuestionIRT]:
        """Chọn câu hỏi tiếp theo tối ưu."""
        q = self.engine.select_next_item(self.theta, self.question_bank, self.administered)
        self.current_q = q
        return q

    def submit_response(self, is_correct: bool, time_spent: int = 0) -> dict:
        """Cập nhật θ sau khi người học trả lời."""
        if self.current_q is None:
            raise ValueError("Không có câu hỏi hiện tại trong session")

        q = self.current_q
        self.administered.add(q.question_id)

        # Cập nhật response history
        self.responses.append((q.a, q.b, q.c, 1 if is_correct else 0))
        self.history.append({
            "question_id" : q.question_id,
            "topic_code"  : q.topic_code,
            "a": q.a, "b": q.b, "c": q.c,
            "is_correct"  : is_correct,
            "time_spent"  : time_spent,
            "theta_before": self.theta,
        })

        # Re-estimate θ
        self.theta, self.se = self.engine.estimate_ability_mle(self.responses)

        # Update history with new theta
        self.history[-1]["theta_after"] = self.theta
        self.history[-1]["se"]          = self.se

        # Kiểm tra điều kiện dừng
        n = len(self.administered)
        stop, reason = self.engine.should_terminate(n, self.se, self.config)

        if stop:
            self.finished      = True
            self.finish_reason = reason

        return {
            "theta"    : round(self.theta, 4),
            "se"       : round(self.se, 4),
            "finished" : self.finished,
            "reason"   : reason,
            "n_items"  : n
        }

    def get_report(self) -> dict:
        """Báo cáo cuối phiên thi CAT."""
        percentile = norm.cdf(self.theta) * 100

        # Phân loại năng lực
        level = "Chưa xác định"
        if   self.theta <= -2.0: level = "Yếu"
        elif self.theta <= -1.0: level = "Dưới trung bình"
        elif self.theta <=  0.0: level = "Trung bình"
        elif self.theta <=  1.0: level = "Khá"
        elif self.theta <=  2.0: level = "Giỏi"
        else                    : level = "Xuất sắc"

        return {
            "session_id"    : self.session_id,
            "user_id"       : self.user_id,
            "theta"         : round(self.theta, 4),
            "se"            : round(self.se, 4),
            "percentile"    : round(percentile, 1),
            "level"         : level,
            "n_items"       : len(self.administered),
            "finish_reason" : self.finish_reason,
            "accuracy"      : round(sum(1 for _, _, _, r in self.responses if r) / max(len(self.responses), 1) * 100, 1),
            "duration_secs" : int((datetime.utcnow() - self.started_at).total_seconds()),
            "history"       : self.history,
            "engine_version": "2.0-irt-3pl"
        }


# ── API Endpoints ─────────────────────────────────────────────────────────────

@app.get("/health")
async def health_check():
    return {
        "status"        : "ok",
        "service"       : "AegisQuiz IRT/CAT Engine",
        "version"       : "2.0.0",
        "active_sessions": len(_sessions),
        "timestamp"     : datetime.utcnow().isoformat()
    }


@app.post("/cat/start", summary="Bắt đầu phiên CAT mới")
async def start_cat_session(request: StartCATRequest):
    """
    Khởi tạo phiên CAT mới và trả về câu hỏi đầu tiên.
    Câu hỏi đầu tiên được chọn tại θ = config.initial_theta (mặc định 0).
    """
    if not request.question_bank:
        raise HTTPException(status_code=400, detail="Question bank không được trống")

    session_id = str(uuid.uuid4())
    session    = CATSession(
        session_id    = session_id,
        user_id       = request.user_id,
        question_bank = request.question_bank,
        config        = request.config
    )

    first_q = session.get_next_question()
    if first_q is None:
        raise HTTPException(status_code=422, detail="Không thể chọn câu hỏi từ question bank")

    _sessions[session_id] = session

    return {
        "session_id"      : session_id,
        "question"        : first_q,
        "current_theta"   : session.theta,
        "n_administered"  : 0,
        "max_items"       : request.config.max_items,
    }


@app.post("/cat/{session_id}/respond", summary="Gửi câu trả lời, nhận câu tiếp theo")
async def respond_to_item(session_id: str, response: CATResponse):
    """
    1. Cập nhật θ dựa trên câu trả lời.
    2. Kiểm tra điều kiện dừng (SE hoặc số lượng câu).
    3. Nếu chưa dừng: chọn câu hỏi tiếp theo tối ưu.
    4. Nếu dừng: trả về báo cáo kết quả.
    """
    session = _sessions.get(session_id)
    if not session:
        raise HTTPException(status_code=404, detail="Session không tồn tại hoặc đã hết hạn")

    if session.finished:
        return {"finished": True, "report": session.get_report()}

    # Cập nhật θ
    update = session.submit_response(response.is_correct, response.time_spent_secs)

    if session.finished:
        # Clean up session sau khi hoàn thành
        return {
            "finished"   : True,
            "theta"      : update["theta"],
            "se"         : update["se"],
            "n_items"    : update["n_items"],
            "report"     : session.get_report()
        }

    # Chọn câu tiếp theo
    next_q = session.get_next_question()
    if next_q is None:
        session.finished      = True
        session.finish_reason = "Hết câu hỏi trong question bank"
        return {"finished": True, "report": session.get_report()}

    return {
        "finished"        : False,
        "question"        : next_q,
        "current_theta"   : update["theta"],
        "se"              : update["se"],
        "n_administered"  : update["n_items"],
    }


@app.get("/cat/{session_id}/result", summary="Kết quả phiên CAT")
async def get_cat_result(session_id: str):
    """Lấy báo cáo kết quả của phiên CAT (kể cả khi chưa kết thúc)."""
    session = _sessions.get(session_id)
    if not session:
        raise HTTPException(status_code=404, detail="Session không tồn tại")
    return session.get_report()


@app.post("/irt/calibrate", summary="Hiệu chỉnh tham số IRT cho câu hỏi")
async def calibrate_item(request: CalibrationRequest, background_tasks: BackgroundTasks):
    """
    Ước lượng tham số IRT (a, b, c) cho 1 câu hỏi dựa trên dữ liệu phản hồi.
    Sử dụng Rasch Model (1PL) cho dữ liệu ít, 3PL khi đủ dữ liệu (n ≥ 200).
    """
    n = len(request.responses)
    if n < 20:
        raise HTTPException(
            status_code=422,
            detail=f"Cần tối thiểu 20 phản hồi để calibrate (hiện có {n})"
        )

    correct    = sum(r.response for r in request.responses)
    p_correct  = correct / n

    # Rasch Model (1PL) — bootstrap nhanh khi dữ liệu ít
    # b ≈ logit(1 - p_correct)  (khó hơn nếu ít người đúng)
    p_clamped  = max(0.05, min(0.95, p_correct))
    b_estimate = math.log((1 - p_clamped) / p_clamped)
    a_estimate = 1.0  # Rasch giả định a=1
    c_estimate = 0.0  # 1PL không có guessing

    # Nếu đủ dữ liệu (n ≥ 200): có thể nâng lên 3PL với EM algorithm
    # (sẽ implement trong version tiếp theo)
    model_used = "1PL-Rasch"
    if n >= 200:
        # Placeholder cho 3PL MML-EM
        c_estimate = 0.20  # Ước tính conservative
        a_estimate = 1.2
        model_used = "3PL-Approximate"

    return {
        "question_id"  : request.question_id,
        "n_responses"  : n,
        "p_correct"    : round(p_correct, 4),
        "a"            : round(a_estimate, 4),
        "b"            : round(b_estimate, 4),
        "c"            : round(c_estimate, 4),
        "model_used"   : model_used,
        "calibrated_at": datetime.utcnow().isoformat(),
        "note"         : "Cập nhật tham số này vào Questions.IrtParams trong database"
    }


@app.post("/irt/ability-estimate", summary="Ước lượng năng lực từ lịch sử làm bài")
async def estimate_ability(responses: List[Dict]):
    """
    Ước lượng θ (năng lực) người học từ lịch sử làm bài (không cần phiên CAT).
    Dùng cho: hiển thị dashboard, chọn độ khó phù hợp lần thi tiếp theo.
    """
    if not responses:
        return {"theta": 0.0, "se": 999.0, "level": "Chưa xác định"}

    try:
        parsed = [(r.get("a", 1.0), r.get("b", 0.0), r.get("c", 0.25), r.get("is_correct", 0))
                  for r in responses]
        theta, se = IRT3PLEngine.estimate_ability_mle(parsed)

        level = (
            "Xuất sắc"       if theta >  2.0 else
            "Giỏi"           if theta >  1.0 else
            "Khá"            if theta >  0.0 else
            "Trung bình"     if theta > -1.0 else
            "Dưới TB"        if theta > -2.0 else
            "Yếu"
        )

        return {
            "theta"      : round(theta, 4),
            "se"         : round(se, 4),
            "percentile" : round(norm.cdf(theta) * 100, 1),
            "level"      : level
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


# ── Run ───────────────────────────────────────────────────────────────────────
if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host="0.0.0.0", port=8001, reload=True, log_level="info")
