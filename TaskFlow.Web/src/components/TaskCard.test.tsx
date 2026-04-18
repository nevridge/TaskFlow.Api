import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { TaskCard } from './TaskCard'
import type { TaskItemResponseDto } from '@/api/client/types.gen'

const task: TaskItemResponseDto = {
  id: 1,
  title: 'Fix login bug',
  description: 'Users cannot log in',
  status: 'todo',
  priority: 'high',
  dueDate: '2026-05-01T00:00:00Z',
  isComplete: false,
}

function renderCard(onEdit = vi.fn(), onDelete = vi.fn()) {
  return render(
    <MemoryRouter>
      <TaskCard task={task} onEdit={onEdit} onDelete={onDelete} />
    </MemoryRouter>
  )
}

describe('TaskCard', () => {
  it('renders the task title', () => {
    renderCard()
    expect(screen.getByText('Fix login bug')).toBeInTheDocument()
  })

  it('renders status and priority badges', () => {
    renderCard()
    expect(screen.getByText('todo')).toBeInTheDocument()
    expect(screen.getByText('high')).toBeInTheDocument()
  })

  it('calls onEdit when edit button is clicked', async () => {
    const onEdit = vi.fn()
    renderCard(onEdit)
    await userEvent.click(screen.getByRole('button', { name: /edit/i }))
    expect(onEdit).toHaveBeenCalledWith(task)
  })

  it('calls onDelete when delete button is clicked', async () => {
    const onDelete = vi.fn()
    renderCard(vi.fn(), onDelete)
    await userEvent.click(screen.getByRole('button', { name: /delete/i }))
    expect(onDelete).toHaveBeenCalledWith(task)
  })
})
