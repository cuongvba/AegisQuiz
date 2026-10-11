import React, { useState } from 'react';
import { useParams } from 'react-router-dom';
import { MobileAppProvider } from '../context/MobileAppContext';
import { MobileShell } from '../components/MobileShell';
import { MobileHomePage } from './MobileHomePage';
import { MobilePracticeRoom } from '../components/MobilePracticeRoom';
import { MobileOfflineManager } from '../components/MobileOfflineManager';
import { MobileProfileView } from '../components/MobileProfileView';
import { OfflineExamPackage } from '@/services/offline-exam.service';

const MobileAppContent: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'home' | 'exam' | 'offline' | 'profile'>('home');
  const [activePracticeTopic, setActivePracticeTopic] = useState<{ id: string; name: string } | null>(null);

  if (activePracticeTopic) {
    return (
      <MobilePracticeRoom
        topicId={activePracticeTopic.id}
        topicName={activePracticeTopic.name}
        onExit={() => setActivePracticeTopic(null)}
      />
    );
  }

  return (
    <MobileShell activeTab={activeTab} onTabChange={setActiveTab}>
      {activeTab === 'home' && (
        <MobileHomePage
          onStartTopic={(id, name) => setActivePracticeTopic({ id, name })}
          onStartFullMockExam={() => setActivePracticeTopic({ id: 'ALL', name: 'Thi Thử Sát Hạch Toàn Diện' })}
        />
      )}

      {activeTab === 'exam' && (
        <MobileHomePage
          onStartTopic={(id, name) => setActivePracticeTopic({ id, name })}
          onStartFullMockExam={() => setActivePracticeTopic({ id: 'ALL', name: 'Thi Thử Sát Hạch Toàn Diện' })}
        />
      )}

      {activeTab === 'offline' && (
        <MobileOfflineManager
          onStartOfflineExam={(pkg: OfflineExamPackage) => setActivePracticeTopic({ id: pkg.examId, name: pkg.title })}
        />
      )}

      {activeTab === 'profile' && <MobileProfileView />}
    </MobileShell>
  );
};

export const MobileAppRootPage: React.FC = () => {
  const { appVariant } = useParams<{ appVariant?: string }>();

  return (
    <MobileAppProvider initialVariantId={appVariant}>
      <MobileAppContent />
    </MobileAppProvider>
  );
};

export default MobileAppRootPage;
