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
  const [activePracticeTopic, setActivePracticeTopic] = useState<string | null>(null);

  if (activePracticeTopic) {
    return (
      <MobilePracticeRoom
        topicName={activePracticeTopic}
        onExit={() => setActivePracticeTopic(null)}
      />
    );
  }

  return (
    <MobileShell activeTab={activeTab} onTabChange={setActiveTab}>
      {activeTab === 'home' && (
        <MobileHomePage
          onStartTopic={(_, name) => setActivePracticeTopic(name)}
          onStartFullMockExam={() => setActivePracticeTopic('Thi Thử Sát Hạch Toàn Diện')}
        />
      )}

      {activeTab === 'exam' && (
        <MobileHomePage
          onStartTopic={(_, name) => setActivePracticeTopic(name)}
          onStartFullMockExam={() => setActivePracticeTopic('Thi Thử Sát Hạch Toàn Diện')}
        />
      )}

      {activeTab === 'offline' && (
        <MobileOfflineManager
          onStartOfflineExam={(pkg: OfflineExamPackage) => setActivePracticeTopic(pkg.title)}
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
