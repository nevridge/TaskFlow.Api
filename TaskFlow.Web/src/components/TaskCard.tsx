import { Link } from 'react-router-dom'
import { cn, formatDate } from '@/lib/utils'
import type { TaskItemResponseDto } from '@/api/client/types.gen'

interface Props {
  task: TaskItemResponseDto
  onEdit: (task: TaskItemResponseDto) => void
  onDelete: (task: TaskItemResponseDto) => void
}

const priorityClass: Record<string, string> = {
  low: 'bg-slate-100 text-slate-700',
  medium: 'bg-yellow-100 text-yellow-800',
  high: 'bg-red-100 text-red-700',
}

const statusClass: Record<string, string> = {
  draft: 'bg-slate-100 text-slate-700',
  todo: 'bg-blue-100 text-blue-700',
  completed: 'bg-green-100 text-green-700',
}

export function TaskCard({ task, onEdit, onDelete }: Props) {
  const status = String(task.status ?? 'draft').toLowerCase()
  const priority = String(task.priority ?? 'low').toLowerCase()

  return (
    <div className="rounded-lg border bg-white p-4 shadow-sm flex flex-col gap-2">
      <div className="flex items-start justify-between gap-2">
        <Link
          to={`/tasks/${task.id}`}
          className="font-semibold text-slate-900 hover:underline"
        >
          {task.title}
        </Link>
        <div className="flex gap-1 shrink-0">
          <button
            aria-label="Edit"
            onClick={() => onEdit(task)}
            className="rounded px-2 py-1 text-xs text-slate-600 hover:bg-slate-100"
          >
            Edit
          </button>
          <button
            aria-label="Delete"
            onClick={() => onDelete(task)}
            className="rounded px-2 py-1 text-xs text-red-600 hover:bg-red-50"
          >
            Delete
          </button>
        </div>
      </div>
      <div className="flex gap-2 flex-wrap">
        <span className={cn('rounded-full px-2 py-0.5 text-xs font-medium', statusClass[status] ?? statusClass.draft)}>
          {status}
        </span>
        <span className={cn('rounded-full px-2 py-0.5 text-xs font-medium', priorityClass[priority] ?? priorityClass.low)}>
          {priority}
        </span>
      </div>
      {task.dueDate && (
        <p className="text-xs text-slate-500">Due {formatDate(task.dueDate)}</p>
      )}
    </div>
  )
}
