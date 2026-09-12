import { MapPin, CheckCircle, Lock, Play, Star } from 'lucide-react';

export function PathExplorer() {
  // MOCK DATA for Visual Skeleton
  const mockNodes = [
    { id: 1, title: 'Nhập môn React', status: 'completed', score: 100 },
    { id: 2, title: 'State & Props', status: 'completed', score: 95 },
    { id: 3, title: 'React Hooks', status: 'active', score: 0 },
    { id: 4, title: 'Context API', status: 'locked', score: 0 },
    { id: 5, title: 'Redux Foundation', status: 'locked', score: 0 },
  ];

  return (
    <div className="max-w-7xl mx-auto px-4 py-8 pb-24">
      <div className="text-center mb-16">
        <h1 className="text-4xl font-black text-slate-900 mb-4 inline-flex items-center gap-3">
          <MapPin className="text-indigo-600" size={40} /> Bản đồ Lộ trình (Path Explorer)
        </h1>
        <p className="text-slate-500 font-medium text-lg max-w-2xl mx-auto">
          Hoàn thành từng trạm (Node) để mở khóa những bài học khó hơn. Vượt qua ải cuối nhận ngay chứng chỉ chuyên gia!
        </p>
      </div>

      <div className="relative max-w-3xl mx-auto py-10">
        {/* Draw Line connecting nodes */}
        <div className="absolute top-0 bottom-0 left-1/2 w-1.5 -translate-x-1/2 bg-slate-200 z-0 border-x border-slate-300"></div>

        <div className="flex flex-col gap-12 relative z-10 w-full pl-6 pr-6">
          {mockNodes.map((node, index) => {
            const isLeft = index % 2 === 0;
            const isCompleted = node.status === 'completed';
            const isActive = node.status === 'active';
            const isLocked = node.status === 'locked';

            return (
              <div 
                key={node.id} 
                className={`flex items-center justify-between w-full relative transition-all duration-300 hover:-translate-y-1 ${
                  isLeft ? 'flex-row-reverse md:flex-row' : 'flex-row md:flex-row-reverse'
                }`}
              >
                {/* Dummy Space for Alternating Layout */}
                <div className="hidden md:block w-[45%]"></div>

                {/* The Node Content Card */}
                <div 
                  className={`w-full md:w-[45%] rounded-3xl p-6 shadow-xl border-4 ${
                    isCompleted 
                      ? 'bg-white border-emerald-500 shadow-emerald-500/20' 
                      : isActive
                        ? 'bg-indigo-600 border-indigo-700 text-white shadow-indigo-600/30 ring-4 ring-indigo-200'
                        : 'bg-slate-100 border-slate-300 opacity-60'
                  }`}
                >
                  <div className="flex justify-between items-start mb-4">
                    <span className={`px-3 py-1 rounded-full text-xs font-black uppercase tracking-wider ${
                      isCompleted ? 'bg-emerald-100 text-emerald-800' :
                      isActive ? 'bg-indigo-400 text-white' :
                      'bg-slate-200 text-slate-500'
                    }`}>
                      Trạm số 0{node.id}
                    </span>
                    
                    {isCompleted && <CheckCircle size={24} className="text-emerald-500" />}
                    {isActive && <Play size={24} fill="currentColor" />}
                    {isLocked && <Lock size={24} className="text-slate-400" />}
                  </div>
                  
                  <h3 className={`text-xl font-bold mb-2 ${isActive ? 'text-white' : 'text-slate-900'}`}>
                    {node.title}
                  </h3>
                  
                  <p className={`text-sm mb-6 ${isActive ? 'text-indigo-200' : 'text-slate-500'}`}>
                    {isCompleted ? `Bạn đã vượt qua xuất sắc đạt ${node.score}/100 điểm.` : 
                     isActive ? 'Trạm luyện tập này đang chờ bạn chinh phục!' : 
                     'Hãy hoàn thành trạm trước để mở khóa nha.'}
                  </p>

                  <button 
                    disabled={isLocked}
                    className={`w-full py-3 rounded-xl font-black text-sm transition-transform shadow-sm flex items-center justify-center gap-2 ${
                      isCompleted 
                        ? 'bg-slate-100 text-slate-700 hover:bg-slate-200' 
                        : isActive
                          ? 'bg-white text-indigo-700 hover:bg-slate-50 hover:shadow-md'
                          : 'bg-transparent text-slate-400 border-2 border-slate-300'
                    }`}
                  >
                    {isCompleted ? 'Xem lại kết quả' : 
                     isActive ? 'Vào chiến ngay' : 
                     'Chưa thể vào'}
                  </button>
                </div>

                {/* Center Node Graphic / Connect to Line */}
                <div className={`absolute left-1/2 -translate-x-1/2 w-12 h-12 rounded-full border-4 shadow-lg flex items-center justify-center z-20 ${
                  isCompleted ? 'bg-emerald-500 border-white text-white' :
                  isActive ? 'bg-white border-indigo-600 text-indigo-600 animate-pulse' :
                  'bg-slate-200 border-white text-slate-400'
                }`}>
                  <Star size={16} fill="currentColor" />
                </div>
              </div>
            )
          })}
        </div>
        
      </div>
    </div>
  );
}