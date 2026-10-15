import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import LoginForm from '@/components/LoginForm.vue'

describe('LoginForm', () => {
  it('emits submit with valid credentials', async () => {
    const wrapper = mount(LoginForm)
    await wrapper.find('#login-email').setValue('user@example.com')
    await wrapper.find('#login-password').setValue('secret123')
    await wrapper.find('form').trigger('submit.prevent')
    expect(wrapper.emitted('submit')).toBeTruthy()
    expect(wrapper.emitted('submit')![0][0]).toEqual({
      email: 'user@example.com',
      password: 'secret123'
    })
  })

  it('shows validation errors for bad input', async () => {
    const wrapper = mount(LoginForm)
    await wrapper.find('#login-email').setValue('not-an-email')
    await wrapper.find('#login-password').setValue('123')
    await wrapper.find('form').trigger('submit.prevent')
    expect(wrapper.emitted('submit')).toBeFalsy()
    expect(wrapper.text()).toContain('valid email')
  })

  it('requires password', async () => {
    const wrapper = mount(LoginForm)
    await wrapper.find('#login-email').setValue('user@example.com')
    await wrapper.find('#login-password').setValue('')
    await wrapper.find('form').trigger('submit.prevent')
    expect(wrapper.emitted('submit')).toBeFalsy()
  })
})
