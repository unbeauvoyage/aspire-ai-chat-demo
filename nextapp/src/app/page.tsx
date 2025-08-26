import Link from "next/link";

export default function Home() {
  return (
    <div className="font-sans min-h-screen p-8 sm:p-20">
      <main className="max-w-3xl mx-auto">
        <h1 className="text-2xl font-semibold mb-6">Master Tutor</h1>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Link href="/tutor/goal" className="block p-6 border rounded hover:bg-gray-50 dark:hover:bg-gray-900">
            <div className="font-medium mb-1">Create Goal</div>
            <div className="text-sm text-gray-600">Define what to learn; get a goalId</div>
          </Link>
          <Link href="/tutor/concepts/1" className="block p-6 border rounded hover:bg-gray-50 dark:hover:bg-gray-900">
            <div className="font-medium mb-1">Concepts</div>
            <div className="text-sm text-gray-600">Generate concepts for a goal (replace 1 with your goalId)</div>
          </Link>
          <Link href="/tutor/quiz/1" className="block p-6 border rounded hover:bg-gray-50 dark:hover:bg-gray-900">
            <div className="font-medium mb-1">Quiz</div>
            <div className="text-sm text-gray-600">Generate a quiz for a concept (replace 1 with your conceptId)</div>
          </Link>
          <Link href="/study" className="block p-6 border rounded hover:bg-gray-50 dark:hover:bg-gray-900">
            <div className="font-medium mb-1">Study Assistant</div>
            <div className="text-sm text-gray-600">Interactive chat-based assistant</div>
          </Link>
        </div>
      </main>
    </div>
  );
}
