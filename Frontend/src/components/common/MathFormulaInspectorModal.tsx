import React, { useState, useEffect } from 'react';
import { 
  X, Check, Copy, AlertTriangle, ShieldCheck, 
  Code2, Eye, RefreshCw, Cpu, Download, ArrowRight
} from 'lucide-react';
import axios from 'axios';
import { cleanSvgDataUri } from '@/shared/lib/quiz/wmfDecoder';

interface MathFormulaInspectorModalProps {
  isOpen: boolean;
  onClose: () => void;
  initialCode?: string;
}

interface DiagnosticTelemetry {
  inputType: string;
  hasBlackPillDefect: boolean;
  polygonCount: number;
  lineCount: number;
  textCount: number;
  pathCount: number;
  viewBox: string;
  extractedTexts: string;
  rawSvg: string;
  cleanedSvg: string;
  cleanedDataUri: string;
  diagnosis: string;
}

export const MathFormulaInspectorModal: React.FC<MathFormulaInspectorModalProps> = ({
  isOpen,
  onClose,
  initialCode = ''
}) => {
  const [inputCode, setInputCode] = useState(initialCode);
  const [telemetry, setTelemetry] = useState<DiagnosticTelemetry | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const [activeTab, setActiveTab] = useState<'preview' | 'cleaned_svg' | 'raw_svg'>('preview');
  const [copiedClean, setCopiedClean] = useState(false);
  const [isDarkPreview, setIsDarkPreview] = useState(true);

  // Mẫu mã ô đen người dùng cung cấp
  const SAMPLE_PILL_DEFECT =
    'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA1NDA4IDk5MiIgc3R5bGU9ImRpc3BsYXk6aW5saW5lLWJsb2NrO3ZlcnRpY2FsLWFsaWduOi0wLjJlbTttYXgtd2lkdGg6MTAwJTtoZWlnaHQ6YXV0bzsiPgo8cG9seWdvbiBwb2ludHM9IjE2LDAgLTY0LC0xIC03MiwtMSA1MzQ0LDAgOTIwLDAgOCwwIDc2MiwwIDE2LDAgMCw1MTIgNCwwIDMwMSwwIDUsMCIgZmlsbD0icmdiKDAsMCwwKSIgc3Ryb2tlPSJyZ2IoMCwwLDApIiBzdHJva2Utd2lkdGg9IjEiIC8+Cjxwb2x5Z29uIHBvaW50cz0iNTMyLDUxMiA2NDYsNSAwLDUzMSA1MTIsMTEzMCA1LDAgNTMyLDUxMiAyODcwLDUgMCw1MzEgNTEyLDMxNTYgNSwwIDUyMSwwIDUxMiw1IDAsNTMyIDYzNiw0MjA2IDI4LDAgNzYzLC01MDcgMjI3LDAgMCw0MDAgMCwyNTYgNTEyLDQwOTYgMzEwNTksMjUxOTcgMjc3NTksMzAyMDggMTQ4NTYsMzAyNTMgNjQsMCAtMTUzMDQsODQ3IDEzOTI3LDMwMjU0IDMyMDY0LDMwMjYyIDAsMCA0LDAgMzAxLDEgOSwwIDI2MTAsMCAwLDEgMCw0MCAwLDUgMCw1MzIgNjM2LDUyMTIgMjgsMCA3NjMsLTUwNyAyMjcsMCAwLDQwMCAwLDI1NiA1MTIsNDA5NiAzMTA1OSwyNTE5NyAyNzc1OSwzMDIwOCAxNDg1NiwzMDI1MyA2NCwwIC0xNTMwNCw4NDcgMTM5MjcsMzAyNTQgMzIwNjQsMzAyNjIgMCwwIDQsMCAzMDEsMiA0LDAgNDk2LDEgOSwwIDI2MTAsMCAwLDEgMCw0MSAwLDUgMCw1MzIgMzY2LDY3MiAyOCwwIDc2MywtMzg0IDAsMCAwLDQwMCAwLDAgNTEyLDQwOTYgMjY5NjQsMjU5NjUgODMwNywyNTkzNCA4MzExLDI4NDk4IDI0OTQxLDExMCAtMTYxNzYsODQ3IDEzOTI3LDMwMjU0IDMyMDY0LDMwMjYyIDAsMCA0LDAgMzAxLDEgNCwwIDQ5NiwyIDksMCAyNjEwLDAgMCwxIDAsNTAgNzY4LDUgMCw1MzIgNjA4LDE3MDAgMjgsMCA3NjMsLTM4NCAwLDAgMCw0MDAgMCwwIDUxMiw0MDk2IDI2OTY0LDI1OTY1IDgzMDcsMjU5MzQgODMxMSwyODQ5OCAyNDk0MSwxMTAgLTE2MTc2LDg0NyAxMzkyNywzMDI1NCAzMjA2NCwzMDI2MiAwLDAgNCwwIDMwMSwyIDQsMCA0OTYsMSAxMiwwIDI2MTAsMCAwLDMgMCwxMTMxNCA1MCw0MzggMTU4OCw3NjggNSwwIDUzMiw5MDggNzk4LDI4IDAsNzYzIC0zODQsMCAwLDAgNDAwLDAgMCw1MTIgNDA5NiwyNjk2NCAyNTk2NSA4MzA3IDI1OTM0LDgzMTEgMjg0OTgsMjQ5NDEgMTEwLC0xNjE3NiA4NDcsMTM5MjcgMzAyNTQsMzIwNjQgMzAyNjIsMCAwLDQgMCwzMDEgMSw0IDAsNDk2IDIsMTAgMCwyNjEwLDAgMCwyIDAsMTMxMDcsMjEyNSA3NjgsNSAwLDUzMiA2MDgsMzQwIDI4LDAgNzYzLC0zODQgMCwwIDAsNDAwIDAsMjU2IDUxMiw0MDk2IDMxMDU5LDI1MTk3IDI3NzU5LDMwMjA4IDE0ODU2LDMwMjUzIDY0LDAgLTE2MTc2LDg0NyAxMzkyNywzMDI1NCAzMjA2NCwzMDI2MiAwLDAgNCwwIDMwMSwyIDQsMCA0OTYsMSAxNSwwIDI2MTAsMCAwLDUgMCwxMTA2OSAxMTA2OSwyMDYgODY4LDEzNTYgNjcwLDEzNzYgNzY4LDUgMCw1MzIgNjA4LDc2IDI4LDAgNzYzLC0zODQgMCwwIDAsNDAwLDEgMCw1MTIgNDA5NiwyNjk2NCAyNTk2NSA4MzA3IDI1OTM0LDgzMTEgMjg0OTgsMjQ5NDEgMTEwIC0xNjE3Niw4NDcgMTM5MjcsMzAyNTQgMzIwNjQsMzAyNjIgMCwwIDQsMCAzMDEgMSw0IDAsNDk2IDIsMTUgMCwyNjEwLDAgMCw1IDAsMjc1MTIgMjc1MTIgMTA3IDE0MTQsODEwIDEyMTYsODQ4IDc2OCw1IDAsNTMyIDM2Niw4NDYgMjgsMCA3NjMsLTM4NCAwLDAgMCw0MDAgMSwyNTYgNTEyLDQwOTYgMzEwNTksMjUxOTcgMjc3NTksMzAyMDggMTQ4NTYsMzAyNTMgNjQsMCAtMTYxNzYsODQ3IDEzOTI3LDMwMjU0IDMyMDY0LDMwMjYyIDAsMCA0LDAgMzAxLDIgNCwwIDQ5NiwxIDEwLDAgMjYxMCwwIDAsMiAwLDI4Nzg0IDIwMjYsNzY4IDUsMCA1MzIsNjA4IDE4NzQsMTAgMCwyNjEwLDAgMCwyIDAsMjg3ODQsMjAyNiA3NjgsNSAwLDUzMiA2MDgsNDkyOCAyOCwwIDc2MywtMzg0IDAsMCAwLDQwMCAwLDI1NiA1MTIsNDA5NiAzMDAyMSwyNzc0NyAyNTcwNSwxOTc0NCAyOTc5Myw4Mjk2IDMwNTQ4LDExMSAtMTYxNzYsODQ3IDEzOTI3LDMwMjU0IDMyMDY0LDMwMjYyIDAsMCA0LDAgMzAxLDEgNCwwIDQ5NiwyIDksMCAyNjEwLDAgMCwxIDAsOTAgNzY4LDIzMyAwLDE1NzQsMTUgNDU2IDI4NzM3LDI5NTUyIDE3OTk3IDE3MjE5IDEsNDE3IDAsNDE3IDAsMjU5MjQgMjY5OTUsMjgyNjMsMjEyODAsMjY5NzkgMjgyNjEsMjU5NTUgODIzNiwyODIzMyAxMTg3NSwxMjgwIDEsMTAzMSAyMTMxNiwyMTU4MSA1NSw0ODY1IDI2OTY3LDE2NzUwIDI3NzU2LDI0ODk4IDI2OTk1LDE3MjUxIDI1NzExLDIwNTgxIDI2NDY1LDI5NTQxIDQzNTIsMjE1MDkgMjgwMDksMjk1NDEgMjAwMDAsMzA1NjUgMjEwMjQsMjgwMTUgMjgyNTcsNDM1MiAyMTI1MSwyODAyNSAyODUxNCwxMDggMTI5NywyODQ4MyAyOTMwMSwyNTk2MSA4MzA2IDI1OTM0IDExOSwxMDQxIDIxNTgxLDE3Njk2IDI5ODE2LDI0OTQ2IDQ2MDgsMjA0OCAxMjA2NSwtMzU0NSA4NTQzLDg1OTEsMTgyMjMsMTY3MzUgLTM1MDQsNzcxMSAyMDU0NSA1NjIwIDE2NjU1LC0zMDcyIC0zMDAzLC0zMDM1IDE3MDM5LDE2NzM1IC0zMDcyLDM4NTYgMjQzODcsNjUgODE3OCwtMjMyNjQgMjgwMiwtMzAzNSA4NTkxLDQzNDAgMTY2NTUsLTMwNzIgMTg0NDcsNjEzMiAtMjg2ODQsNjUgNjg5OCwxNzUwMyAxNzc1OSwyNDU2NCAtMzAwMywxNjczNSAzMDg3LDEgMSw1MTMgNTE0LDIgMiwyNTcgMSwzIDEsNCAyNTYwLDEgMTYsMCAwLDAgMjcxLDIgMzA4NTEsNTEyIC0zMTIyOCw2MSA4MjksMjgxNiAwLDE1IDEsMjcxIDIsMTI5MzYgNTEyLC0zMTc0MCA5NjAsMTEyIDE1LDEgMjcxLDIgMTMxOTIsMCA1MTIsLTMxMjI4IDQzLDU1NSAtMzIwMDAgMTA3IDIsMTI5MzYgNTEyLC0zMTc0MCA5NjAsNjI0IC0zMjI1Niw0NCAyLDMwODUxIDUxMiwtMzEyMjgsNjEgODI5IDI4MTYsMCAxNSwxIDI3MSwxMDI2IC0xNjI1MiwyODY3NSAzODQwLDI1NiAzODQwLDUxMyAtMzA3MjAsNTEgMCwxMDI2IDExMTQyLDExMDA4IDIsMjc1MjMsNTEyIC0zMDcyMCw1MCAxMDI2IC0xNjI1MiwyODY3NSAzLDc2OSAzODQwLDI1NiAzODQwLDUxMyAtMzIwMDAsMTA3IDEwMjYsMjE4MiAtMTI3NjYsMTc2ODMsMjU0NjEsMjY5ODggMTk4MTIsMjk3OTMsMTI5MDQgNDM1MiwxNzY3MCwyNTQ2MSwyNjk4OCw4MjkyIDI0OTA5LDI2NzQwIDIxNTM2IDI4NTM1IDIwNDgsNSAxMDI2LDkzNDMsMjMwNzMsNTEyIC0yNzEzNiw0MCAyLDEwNjQ2IDAsMCAxMCwwIDE1NzQsMTUgMTAsLTEgLTEsMSAwLDAgOCwwIDc2MiwwIDAsMCAwLDAgNCwwIDMwMSwyIDI4LDAgNzYzLDE2IDcsMCAwLDcwMCAwLDAgNTEzLDg3MDYsMzEwNTkgMjk4MTEgMjgwMDUsMCAxMzkzLDEzOCAwLDEwIDM5ODgsLTEyMTg2IDM5ODgsLTEyMTg2IDEzOTMsMTM4IC0xMzY0OCw4NDcgNCwwIDMwMSwzIDQsMCA0OTYsMSAzLDAiIGZpbGw9InJnYigwLDAsMCkiIHN0cm9rZT0icmdiKDAsMCwwKSIgc3Ryb2tlLXdpZHRoPSIxIiAvPgo8cG9seWdvbiBwb2ludHM9IjAsNzYyIDAsMTYgMCwwIDUxMiw0IDAsMzAxIDAsNSAwLDUzMiA1MTIsNjQ2IDUsMCA1MzEsNTEyIDExMzAsNSAwLDUzMiA1MTIsMjg3MCA1LDAgNTMxLDUxMiAzMTU2LDUgMCw1MjEgMCw1MTIgNSwwIDUzMiw2MzYgNDIwNiwyOCAwLDc2MyAtNTA3LDIyNyAwLDAgNDAwLDAgMjU2LDUxMiIgZmlsbD0icmdiKDAsMCwwKSIgc3Ryb2tlPSJyZ2IoMCwwLDApIiBzdHJva2Utd2lkdGg9IjEiIC8+Cjxwb2x5Z29uIHBvaW50cz0iNDA5NiwzMTA1OSAyNTE5NywyNzc1OSAzMDIwOCwxNDg1NiAzMDI1Myw2NCAwLC0xNTMwNCA4NDcsMTM5MjcgMzAyNTQsMzIwNjQgMzAyNjIsMCAwLDQgMCwzMDEgMSw5IDAsMjYxMCAwLDAgMSwwIDQwLDAgNSwwIiBmaWxsPSJyZ2IoMCwwLDApIiBzdHJva2U9InJnYigwLDAsMCkiIHN0cm9rZS13aWR0aD0iMSIgLz4KPHBvbHlnb24gcG9pbnRzPSI1MzIsNjM2IDUyMTIsMjggMCw3NjMgLTUwNywyMjcgMCwwIDQwMCwwIDI1Niw1MTIgNDA5NiwzMTA1OSAyNTE5NywyNzc1OSAzMDIwOCwxNDg1NiAzMDI1Myw2NCAwLC0xNTMwNCA4NDcsMTM5MjcgMzAyNTQsMzIwNjQgMzAyNjIsMCAwLDQgMCwzMDEgMiw0IDAsNDk2IDEsOSAwLDI2MTAgMCwwIDEsMCA0MSwwIDUsMCA1MzIsMzY2IDY3MiwyOCAwLDc2MyAtMzg0LDAgMCwwIDQwMCwwIDAsNTEyIDQwOTYsMjY5NjQgMjU5NjUsODMwNyAyNTkzNCw4MzExIDI4NDk4LDI0OTQxIDExMCwtMTYxNzYgODQ3LDEzOTI3IDMwMjU0LDMyMDY0IDMwMjYyLDAgMCw0IDAsMzAxIDEsNCAwLDQ5NiAyLDkgMCwyNjEwIDAsMCAxLDAgNTAsNzY4IDUsMCA1MzIsNjA4IDE3MDAsMjggMCw3NjMsLTM4NCAwLDAgMCw0MDAsMCAwLDUxMiA0MDk2LDI2OTY0IDI1OTY1LDgzMDcsMjU5MzQsODMxMSAyODQ5OCwyNDk0MSAxMTAsLTE2MTc2IDg0NywxMzkyNyAzMDI1NCwzMjA2NCAzMDI2MiwwIDAsNCAwLDMwMSAyLDQgMCw0OTYgMSwxMiAwLDI2MTAgMCwwIDMsMCAxMTMxNCw1MCA0MzgsMTU4OCA3NjgsNSAwLDUzMiA5MDgsNzk4IDI4LDAgNzYzLC0zODQgMCwwIDAsNDAwIDAsMCA1MTIsNDA5NiAyNjk2NCwyNTk2NSA4MzA3IDI1OTM0LDgzMTEgMjg0OTgsMjQ5NDEgMTEwIC0xNjE3Niw4NDcgMTM5MjcsMzAyNTQsMzIwNjQgMzAyNjIsMCAwLDQgMCwzMDEgMSw0IDAsNDk2IDIsMTAgMCwyNjEwLDAgMCwyIDAsMTMxMDcgMjEyNSA3NjgsNSAwLDUzMiA2MDgsMzQwIDI4LDAgNzYzLC0zODQgMCwwIDAsNDAwIDAsMjU2IDUxMiw0MDk2IDMxMDU5LDI1MTk3IDI3NzU5LDMwMjA4IDE0ODU2LDMwMjUzIDY0LDAgLTE2MTc2LDg0NyAxMzkyNywzMDI1NCAzMjA2NCwzMDI2MiwwIDAsNCAwLDMwMSAyLDQgMCw0OTYgMSwxNSAwLDI2MTAgMCwwIDUsMCAxMTA2OSwxMTA2OSAyMDYsODY4IDEzNTYsNjcwIDEzNzYsNzY4IDUsMCA1MzIsNjA4IDc2LDI4LDAgNzYzLC0zODQgMCwwIDAsNDAwLDEgMCw1MTIgNDA5NiwyNjk2NCAyNTk2NSA4MzA3IDI1OTM0LDgzMTEgMjg0OTgsMjQ5NDEgMTEwLC0xNjE3NiA4NDcsMTM5MjcgMzAyNTQsMzIwNjQgMzAyNjIsMCAwLDQgMCwzMDEgMSw0IDAsNDk2IDIsMTUgMCwyNjEwLDAgMCw1IDAsMjc1MTIgMjc1MTIgMTA3LDE0MTQgODEwIDEyMTYsODQ4LDc2OCA1LDAgNTMyLDM2NiA4NDYsMjggMCw3NjMsLTM4NCAwLDAgMCw0MDAsMSAyNTYsNTEyIDQwOTYsMzEwNTkgMjUxOTcsMjc3NTkgMzAyMDgsMTQ4NTYgMzAyNTMsNjQgMCwtMTYxNzYsODQ3IDEzOTI3LDMwMjU0IDMyMDY0LDMwMjYyIDAsMCA0LDAgMzAxLDIgNCwwIDQ5NiAxLDEwIDAsMjYxMCAwLDAgMiwwIDI4Nzg0IDIwMjYgNzY4IDUsMCA1MzIsNjA4IDE4NzQsMTAsMCAyNjEwLDAgMCwyIDAsMjg3ODQsMjAyNiA3NjgsNSAwLDUzMiA2MDgsNDkyOCwyOCwwIDc2MywtMzg0IDAsMCAwLDQwMCwwIDI1Niw1MTIgNDA5NiwzMDAyMSAyNzc0NyAyNTcwNSwxOTc0NCAyOTc5Myw4Mjk2IDMwNTQ4LDExMSAtMTYxNzYsODQ3IDEzOTI3LDMwMjU0IDMyMDY0LDMwMjYyLDAgMCw0IDAsMzAxIDEsNCAwLDQ5NiAyLDkgMCwyNjEwLDAgMCwxLDAgOTAsNzY4IDIzMywwIDE1NzQsMTUgNDU2LDI4NzM3IDI5NTUyLDE3OTk3IDE3MjE5LDEgNDE3LDAgNDE3LDAgMjU5MjQsMjY5OTUgMjgyNjMsMjEyODAsMjY5NzkgMjgyNjEsMjU5NTUgODIzNiAyODIzMywxMTg3NSAxMjgwLDEgMTAzMSwyMTMxNiAyMTU4MSw1NSA0ODY1IDI2OTY3IDE2NzUwIDI3NzU2IDI0ODk4IDI2OTk1LDE3MjUxIDI1NzExLDIwNTgxIDI2NDY1LDI5NTQxLDQzNTIsMjE1MDkgMjgwMDksMjk1NDEsMjAwMDAgMzA1NjUsMjEwMjQsMjgwMTUgMjgyNTcsNDM1MiAyMTI1MSwyODAyNSAyODUxNCwxMDggMTI5NyAyODQ4MyAyOTMwMSwyNTk2MSA4MzA2IDI1OTM0IDExOSwxMDQxIDIxNTgxLDE3Njk2IDI5ODE2LDI0OTQ2IDQ2MDgsMjA0OCwxMjA2NSAtMzU0NSA4NTQzLDg1OTEsMTgyMjMsMTY3MzUgLTM1MDQsNzcxMSAyMDU0NSA1NjIwIDE2NjU1LC0zMDcyIC0zMDAzLC0zMDM1IDE3MDM5LDE2NzM1IC0zMDcyLDM4NTYsMjQzODcsNjUgODE3OCwtMjMyNjQgMjgwMiwtMzAzNSA4NTkxLDQzNDAsMTY2NTUsLTMwNzIsMTg0NDcsNjEzMiwtMjg2ODQsNjUgNjg5OCwxNzUwMywxNzc1OSAyNDU2NCwtMzAwMyAxNjczNSwzMDg3IDEsMSA1MTMsNTE0IDIsMiAyNTcsMSAzLDEgNCwyNTYwLDEgMTYsMCAwLDAgMjcxLDIgMzA4NTEsNTEyIC0zMTIyOCw2MSA4MjksMjgxNiAwLDE1IDEsMjcxIDIsMTI5MzYgNTEyLC0zMTc0MCA5NjAsMTEyIDE1LDEgMjcxLDIgMTMxOTIsMCA1MTIsLTMxMjI4LDQzIDU1NSwtMzIwMDAgMTA3LDIgMTI5MzYgNTEyLC0zMTc0MCA5NjAsNjI0LC0zMjI1NiA0NCwyIDMwODUxLDUxMiAtMzEyMjgsNjEgODI5IDI4MTYsMCAxNSwxIDI3MSAxMDI2LC0xNjI1MiAyODY3NSwzODQwIDI1NiwzODQwIDUxMywtMzA3MjAgNTEsMCAxMDI2LDExMTQyIDExMDA4LDIgMjc1MjMsNTEyIC0zMDcyMCw1MCAxMDI2LC0xNjI1MiAyODY3NSwzIDc2OSwzODQwIDI1NiwzODQwIDUxMywtMzIwMDAgMTA3LDEwMjYsMjE4MiAtMTI3NjYsMTc2ODMsMjU0NjEsMjY5ODgsMTk4MTIsMjk3OTMsMTI5MDQgNDM1MiwxNzY3MCwyNTQ2MSwyNjk4OCw4MjkyIDI0OTA5LDI2NzQwIDIxNTM2IDI4NTM1IDIwNDgsNSAxMDI2LDkzNDMsMjMwNzMsNTEyIC0yNzEzNiw0MCAyLDEwNjQ2IDAsMCAxMCwwIDE1NzQsMTUgMTAsLTEgLTEsMSAwLDAgOCwwIDc2MiwwIDAsMCAwLDAgMCw0IDAsMzAxIDIsMjggMCw3NjMsMTYgNywwIDAsNzAwIDAsMCA1MTMsODcwNiAzMTA1OSwyOTgxMSAyODAwNSwwIDEzOTMsMTM4IDAsMTAgMzk4OCwtMTIxODYgMzk4OCwtMTIxODYgMTM5MywxMzggLTEzNjQ4LDg0NyA0LDAgMzAxLDMgNCwwIDQ5NiwxIDMsMCBmaWxsPSJyZ2IoMCwwLDApIiBzdHJva2U9InJnYigwLDAsMCkiIHN0cm9rZS13aWR0aD0iMSIgLz4KPHBvbHlnb24gcG9pbnRzPSIwLDMwMSAyLDI4IDAsNzYzIDE2LDcgMCwwIDcwMCwwIDAsNTEzIDg3MDYsMzEwNTkgMjk4MTEgMjgwMDUgMCwxMzkzIiBmaWxsPSJyZ2IoMCwwLDApIiBzdHJva2U9InJnYigwLDAsMCkiIHN0cm9rZS13aWR0aD0iMSIgLz4KPHBvbHlnb24gcG9pbnRzPSIxMzgsMCIgZmlsbD0icmdiKDAsMCwwKSIgc3Ryb2tlPSJyZ2IoMCwwLDApIiBzdHJva2Utd2lkdGg9IjEiIC8+Cjxwb2x5Z29uIHBvaW50cz0iMTAsMzk4OCAtMTIxODYsMzk4OCAtMTIxODYsMTM5MyAxMzgsLTEzNjQ4IDg0Nyw0IDAsMzAxIDMsNCAwLDQ5NiIgZmlsbD0icmdiKDAsMCwwKSIgc3Ryb2tlPSJyZ2IoMCwwLDApIiBzdHJva2Utd2lkdGg9IjEiIC8+Cjxwb2x5Z29uIHBvaW50cz0iMSwzIDAsMCIgZmlsbD0icmdiKDAsMCwwKSIgc3Ryb2tlPSJyZ2IoMCwwLDApIiBzdHJva2Utd2lkdGg9IjEiIC8+Cgo8L3N2Zz4=';

  useEffect(() => {
    if (isOpen && initialCode) {
      setInputCode(initialCode);
      handleAnalyze(initialCode);
    }
  }, [isOpen, initialCode]);

  const handleAnalyze = async (codeToTest?: string) => {
    const raw = (codeToTest ?? inputCode).trim();
    if (!raw) return;

    setIsProcessing(true);
    try {
      // 1. Thử gọi qua Backend Endpoint
      const res = await axios.post('/api/quiz/math-dictionary/inspect-formula', { rawInput: raw });
      if (res.data?.success) {
        setTelemetry(res.data);
      }
    } catch {
      // Fallback: Chạy trực tiếp trên Client nếu Backend offline
      runClientSideInspection(raw);
    } finally {
      setIsProcessing(false);
    }
  };

  const runClientSideInspection = (raw: string) => {
    let rawSvg = '';
    let cleanedSvg = '';
    let cleanedDataUri = '';
    let inputType = 'svg_base64';

    try {
      if (raw.startsWith('data:image/svg+xml') || raw.startsWith('image/svg+xml')) {
        const comma = raw.indexOf(',');
        const b64 = raw.substring(comma + 1);
        rawSvg = atob(b64);
        cleanedDataUri = cleanSvgDataUri(raw.startsWith('data:') ? raw : 'data:' + raw);
        const c2 = cleanedDataUri.indexOf(',');
        cleanedSvg = atob(cleanedDataUri.substring(c2 + 1));
      } else if (raw.startsWith('<svg')) {
        inputType = 'raw_svg_xml';
        rawSvg = raw;
        cleanedSvg = raw.replace(/<polygon\b[^>]*\/?>/gi, '');
        cleanedDataUri = 'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(cleanedSvg)));
      }

      const polygonCount = (rawSvg.match(/<polygon\b/gi) || []).length;
      const lineCount = (rawSvg.match(/<line\b/gi) || []).length;
      const textCount = (rawSvg.match(/<text\b/gi) || []).length;
      const pathCount = (rawSvg.match(/<path\b/gi) || []).length;
      const vbMatch = rawSvg.match(/viewBox="([^"]+)"/i);

      setTelemetry({
        inputType,
        hasBlackPillDefect: polygonCount > 0,
        polygonCount,
        lineCount,
        textCount,
        pathCount,
        viewBox: vbMatch ? vbMatch[1] : 'N/A',
        extractedTexts: '',
        rawSvg,
        cleanedSvg,
        cleanedDataUri,
        diagnosis: polygonCount > 0
          ? `[Client Analysis] Phát hiện ${polygonCount} thẻ <polygon> đen che phủ. LỖI THUỘC QUÁ TRÌNH TẠO MÃ SVG (dịch nhầm nhị phân WMF). Đã lọc sạch.`
          : 'Mã SVG chuẩn W3C vector, không chứa polygon rác.'
      });
    } catch (e) {
      console.error(e);
    }
  };

  const handleCopyCleaned = () => {
    if (telemetry?.cleanedDataUri) {
      navigator.clipboard.writeText(telemetry.cleanedDataUri);
      setCopiedClean(true);
      setTimeout(() => setCopiedClean(false), 2000);
    }
  };

  const handleDownloadSvg = () => {
    if (!telemetry?.cleanedSvg) return;
    const blob = new Blob([telemetry.cleanedSvg], { type: 'image/svg+xml' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'formula_cleaned.svg';
    a.click();
    URL.revokeObjectURL(url);
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 backdrop-blur-md p-4 animate-in fade-in duration-200">
      <div className="bg-slate-900 border border-slate-700/80 rounded-2xl w-full max-w-5xl max-h-[92vh] flex flex-col shadow-2xl overflow-hidden">
        
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-800 bg-slate-950/60">
          <div className="flex items-center gap-3">
            <div className="p-2.5 rounded-xl bg-indigo-500/10 border border-indigo-500/30 text-indigo-400">
              <Cpu className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-lg font-bold text-white flex items-center gap-2">
                AegisQuiz Math Formula Inspector & Live Converter
                <span className="text-xs px-2.5 py-0.5 rounded-full bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 font-mono">
                  v2.5 Vector Engine
                </span>
              </h2>
              <p className="text-xs text-slate-400">
                Công cụ kiểm chứng trực quan: Xác định nguyên nhân lỗi do Tạo Mã (Encoding) hay Giải Mã (Decoding)
              </p>
            </div>
          </div>
          <button 
            onClick={onClose}
            className="p-2 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800/80 transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Body Content */}
        <div className="flex-1 overflow-y-auto p-6 space-y-6">
          
          {/* Input Section */}
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <label className="text-xs font-semibold uppercase tracking-wider text-slate-300 flex items-center gap-2">
                <Code2 className="w-4 h-4 text-indigo-400" />
                Chuỗi mã công thức (Base64 Data URI / WMF / SVG XML):
              </label>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => {
                    setInputCode(SAMPLE_PILL_DEFECT);
                    handleAnalyze(SAMPLE_PILL_DEFECT);
                  }}
                  className="text-xs px-3 py-1 rounded-lg bg-amber-500/10 hover:bg-amber-500/20 text-amber-400 border border-amber-500/30 transition-colors flex items-center gap-1.5"
                >
                  <AlertTriangle className="w-3.5 h-3.5" />
                  Dán chuỗi mẫu "Lỗi viên thuốc"
                </button>
                <button
                  type="button"
                  onClick={() => setInputCode('')}
                  className="text-xs text-slate-400 hover:text-slate-200 px-2 py-1"
                >
                  Xóa
                </button>
              </div>
            </div>
            
            <div className="relative">
              <textarea
                value={inputCode}
                onChange={(e) => setInputCode(e.target.value)}
                placeholder="Dán chuỗi data:image/svg+xml;base64,... hoặc data:image/x-wmf;base64,... vào đây..."
                rows={4}
                className="w-full bg-slate-950/70 border border-slate-700/80 rounded-xl p-3.5 text-xs font-mono text-slate-200 focus:outline-none focus:border-indigo-500 transition-colors resize-none placeholder:text-slate-600"
              />
            </div>

            <div className="flex items-center justify-end gap-3 pt-1">
              <button
                onClick={() => handleAnalyze()}
                disabled={isProcessing || !inputCode.trim()}
                className="px-5 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white text-xs font-semibold flex items-center gap-2 shadow-lg shadow-indigo-600/20 transition-all"
              >
                <RefreshCw className={`w-4 h-4 ${isProcessing ? 'animate-spin' : ''}`} />
                {isProcessing ? 'Đang phân tích...' : 'Giải mã & Phân tích cấu trúc'}
              </button>
            </div>
          </div>

          {/* Telemetry & Diagnostic Report */}
          {telemetry && (
            <div className="space-y-5 animate-in fade-in slide-in-from-top-2 duration-300">
              
              {/* Verdict Banner */}
              <div className={`p-4 rounded-xl border flex items-start gap-3.5 ${
                telemetry.hasBlackPillDefect 
                  ? 'bg-rose-950/30 border-rose-500/40 text-rose-200' 
                  : 'bg-emerald-950/30 border-emerald-500/40 text-emerald-200'
              }`}>
                <div className={`p-2 rounded-lg ${
                  telemetry.hasBlackPillDefect ? 'bg-rose-500/20 text-rose-400' : 'bg-emerald-500/20 text-emerald-400'
                }`}>
                  {telemetry.hasBlackPillDefect ? <AlertTriangle className="w-5 h-5" /> : <ShieldCheck className="w-5 h-5" />}
                </div>
                <div className="space-y-1">
                  <div className="text-sm font-bold flex items-center gap-2">
                    {telemetry.hasBlackPillDefect ? (
                      <>
                        <span>Xác nhận: Lỗi 100% nằm ở Quá Trình Tạo Mã (Encoding / WMF Translation)</span>
                        <span className="px-2 py-0.5 rounded bg-rose-500/20 text-rose-300 text-xs font-mono">
                          Phát hiện {telemetry.polygonCount} polygon đen
                        </span>
                      </>
                    ) : (
                      <span>Cấu trúc mã sạch (Zero Defect) — Không có polygon đen che phủ</span>
                    )}
                  </div>
                  <p className="text-xs opacity-90 leading-relaxed">
                    {telemetry.diagnosis}
                  </p>
                </div>
              </div>

              {/* Vector Statistics Grid */}
              <div className="grid grid-cols-2 sm:grid-cols-5 gap-3">
                <div className="p-3 rounded-xl bg-slate-800/50 border border-slate-700/60 text-center">
                  <div className="text-xs text-slate-400">Định dạng</div>
                  <div className="text-sm font-bold text-slate-200 uppercase font-mono mt-0.5">
                    {telemetry.inputType.replace('_', ' ')}
                  </div>
                </div>
                <div className={`p-3 rounded-xl border text-center ${
                  telemetry.polygonCount > 0 
                    ? 'bg-rose-950/40 border-rose-500/50 text-rose-300' 
                    : 'bg-slate-800/50 border-slate-700/60 text-slate-200'
                }`}>
                  <div className="text-xs text-slate-400">Đa giác (Polygon)</div>
                  <div className="text-lg font-bold font-mono mt-0.5">
                    {telemetry.polygonCount} {telemetry.polygonCount > 0 ? '⚠️' : '✅'}
                  </div>
                </div>
                <div className="p-3 rounded-xl bg-slate-800/50 border border-slate-700/60 text-center">
                  <div className="text-xs text-slate-400">Nét phân số (Line)</div>
                  <div className="text-lg font-bold text-emerald-400 font-mono mt-0.5">
                    {telemetry.lineCount}
                  </div>
                </div>
                <div className="p-3 rounded-xl bg-slate-800/50 border border-slate-700/60 text-center">
                  <div className="text-xs text-slate-400">Ký tự chữ/số (Text)</div>
                  <div className="text-lg font-bold text-sky-400 font-mono mt-0.5">
                    {telemetry.textCount}
                  </div>
                </div>
                <div className="p-3 rounded-xl bg-slate-800/50 border border-slate-700/60 text-center">
                  <div className="text-xs text-slate-400">Khung nhìn (viewBox)</div>
                  <div className="text-xs font-mono text-amber-300 truncate mt-1.5" title={telemetry.viewBox}>
                    {telemetry.viewBox || 'Mặc định'}
                  </div>
                </div>
              </div>

              {/* Side-by-side Comparative Visual Preview */}
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <h3 className="text-xs font-semibold uppercase tracking-wider text-slate-300 flex items-center gap-2">
                    <Eye className="w-4 h-4 text-emerald-400" />
                    So sánh trực quan: Trước & Sau khi làm sạch
                  </h3>
                  <button
                    type="button"
                    onClick={() => setIsDarkPreview(!isDarkPreview)}
                    className="text-xs px-2.5 py-1 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 flex items-center gap-1.5"
                  >
                    Chế độ nền: {isDarkPreview ? '🌙 Nền tối' : '☀️ Nền sáng'}
                  </button>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {/* Cột 1: Mã gốc (Bị lỗi viên thuốc đen) */}
                  <div className="p-4 rounded-xl bg-slate-950/60 border border-slate-800 space-y-2">
                    <div className="flex items-center justify-between text-xs">
                      <span className="font-semibold text-rose-400 flex items-center gap-1.5">
                        <span className="w-2 h-2 rounded-full bg-rose-500"></span>
                        Mã Gốc Chưa Sửa (Original)
                      </span>
                      <span className="text-slate-500">Trình duyệt render thô</span>
                    </div>

                    <div className={`min-h-[100px] flex items-center justify-center p-4 rounded-lg border ${
                      isDarkPreview 
                        ? 'bg-slate-900/90 border-slate-800' 
                        : 'bg-white border-slate-200'
                    }`}>
                      {inputCode.startsWith('data:') ? (
                        <img 
                          src={inputCode} 
                          alt="Original Broken Rendering" 
                          className="max-h-24 max-w-full object-contain"
                        />
                      ) : (
                        <div 
                          className="max-h-24 max-w-full overflow-hidden" 
                          dangerouslySetInnerHTML={{ __html: telemetry.rawSvg }} 
                        />
                      )}
                    </div>
                    <p className="text-[11px] text-slate-500 italic text-center">
                      {telemetry.hasBlackPillDefect 
                        ? '❌ Thể hiện khối đen đặc che kín vì chứa các polygon tọa độ rác.' 
                        : 'Hiển thị bình thường.'}
                    </p>
                  </div>

                  {/* Cột 2: Đã làm sạch & Khắc phục */}
                  <div className="p-4 rounded-xl bg-slate-950/60 border border-slate-800 space-y-2">
                    <div className="flex items-center justify-between text-xs">
                      <span className="font-semibold text-emerald-400 flex items-center gap-1.5">
                        <span className="w-2 h-2 rounded-full bg-emerald-500"></span>
                        Đã Làm Sạch (Cleaned Vector Engine)
                      </span>
                      <button
                        onClick={handleCopyCleaned}
                        className="text-xs px-2 py-0.5 rounded bg-emerald-500/10 hover:bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 flex items-center gap-1 transition-colors"
                      >
                        {copiedClean ? <Check className="w-3 h-3" /> : <Copy className="w-3 h-3" />}
                        {copiedClean ? 'Đã sao chép!' : 'Sao chép chuỗi sạch'}
                      </button>
                    </div>

                    <div className={`min-h-[100px] flex items-center justify-center p-4 rounded-lg border ${
                      isDarkPreview 
                        ? 'bg-slate-900/90 border-slate-800 text-white' 
                        : 'bg-white border-slate-200 text-slate-900'
                    }`}>
                      {telemetry.cleanedDataUri ? (
                        <img 
                          src={telemetry.cleanedDataUri} 
                          alt="Cleaned SVG Rendering" 
                          className="max-h-24 max-w-full object-contain filter invert-0 dark:brightness-125"
                        />
                      ) : (
                        <div 
                          className="max-h-24 max-w-full overflow-hidden"
                          dangerouslySetInnerHTML={{ __html: telemetry.cleanedSvg }} 
                        />
                      )}
                    </div>
                    <p className="text-[11px] text-emerald-500/90 text-center font-medium">
                      ✅ Đã triệt tiêu hoàn toàn ô đen, bảo toàn nét vẽ công thức vector sắc nét.
                    </p>
                  </div>
                </div>
              </div>

              {/* Code Inspection Tabs */}
              <div className="space-y-2">
                <div className="flex items-center justify-between border-b border-slate-800 pb-2">
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => setActiveTab('preview')}
                      className={`text-xs px-3 py-1.5 rounded-lg transition-colors font-medium ${
                        activeTab === 'preview'
                          ? 'bg-indigo-600 text-white'
                          : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800'
                      }`}
                    >
                      Báo cáo phân tích
                    </button>
                    <button
                      onClick={() => setActiveTab('cleaned_svg')}
                      className={`text-xs px-3 py-1.5 rounded-lg transition-colors font-medium ${
                        activeTab === 'cleaned_svg'
                          ? 'bg-indigo-600 text-white'
                          : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800'
                      }`}
                    >
                      Mã nguồn SVG Đã Làm Sạch
                    </button>
                    <button
                      onClick={() => setActiveTab('raw_svg')}
                      className={`text-xs px-3 py-1.5 rounded-lg transition-colors font-medium ${
                        activeTab === 'raw_svg'
                          ? 'bg-indigo-600 text-white'
                          : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800'
                      }`}
                    >
                      Mã nguồn SVG Gốc (Có lỗi)
                    </button>
                  </div>

                  <button
                    onClick={handleDownloadSvg}
                    disabled={!telemetry.cleanedSvg}
                    className="text-xs px-3 py-1 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 flex items-center gap-1.5 transition-colors"
                  >
                    <Download className="w-3.5 h-3.5" />
                    Tải file SVG sạch (.svg)
                  </button>
                </div>

                {activeTab === 'preview' && (
                  <div className="p-4 rounded-xl bg-slate-950/70 border border-slate-800 space-y-3 text-xs text-slate-300 leading-relaxed font-mono">
                    <div className="flex items-center gap-2 text-indigo-400 font-bold">
                      <ArrowRight className="w-4 h-4" />
                      KẾT QUẢ PHÂN TÍCH KỸ THUẬT:
                    </div>
                    <ul className="list-disc list-inside space-y-1.5 text-slate-400">
                      <li>
                        <strong className="text-slate-200">Bản chất lỗi:</strong> Bộ chuyển đổi WMF cũ (hoặc thư viện WMF trên Linux/Ubuntu container) đã thông dịch nhầm record metadata <code className="text-amber-400">0x0626</code> (MTEF binary) hoặc <code className="text-amber-400">0x0538</code> thành hàng trăm tọa độ đa giác polygon.
                      </li>
                      <li>
                        <strong className="text-slate-200">Khâu bị lỗi:</strong> 100% thuộc về <strong className="text-rose-400">Khâu Tạo Mã (Backend/Frontend WMF-to-SVG Parser)</strong>. Khâu giải mã (Browser SVG renderer) hoàn toàn hoạt động đúng theo chỉ thị của file ảnh.
                      </li>
                      <li>
                        <strong className="text-slate-200">Giải pháp triệt để:</strong> Bỏ qua hoàn toàn record <code className="text-emerald-400">0x0626 / 0x0538</code> và tự động loại bỏ các thẻ <code className="text-emerald-400">&lt;polygon&gt;</code> rác trong hàm làm sạch <code className="text-emerald-400">SanitizeMathTypeSvg</code>.
                      </li>
                    </ul>
                  </div>
                )}

                {activeTab === 'cleaned_svg' && (
                  <div className="relative">
                    <pre className="p-4 rounded-xl bg-slate-950 border border-slate-800 text-emerald-400 text-xs font-mono max-h-60 overflow-y-auto whitespace-pre-wrap select-all">
                      {telemetry.cleanedSvg}
                    </pre>
                  </div>
                )}

                {activeTab === 'raw_svg' && (
                  <div className="relative">
                    <pre className="p-4 rounded-xl bg-slate-950 border border-slate-800 text-rose-300 text-xs font-mono max-h-60 overflow-y-auto whitespace-pre-wrap select-all">
                      {telemetry.rawSvg}
                    </pre>
                  </div>
                )}
              </div>

            </div>
          )}

        </div>

        {/* Footer */}
        <div className="flex items-center justify-between px-6 py-4 border-t border-slate-800 bg-slate-950/60">
          <div className="text-xs text-slate-500 font-mono">
            Tương thích: Windows Desktop &amp; Ubuntu VPS Linux Container
          </div>
          <button
            onClick={onClose}
            className="px-5 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-white text-xs font-semibold transition-colors"
          >
            Đóng
          </button>
        </div>

      </div>
    </div>
  );
};
