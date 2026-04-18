import { Routes, Route } from 'react-router-dom'
import { TasksPage } from '@/pages/TasksPage'
import { TaskDetailPage } from '@/pages/TaskDetailPage'

function App() {
  return (
    <Routes>
      <Route path="/" element={<TasksPage />} />
      <Route path="/tasks/:id" element={<TaskDetailPage />} />
    </Routes>
  )
}

export default App
