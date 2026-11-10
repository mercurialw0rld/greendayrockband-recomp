#include "thread_priority.h"
#include <atomic>
#include <rex/hook.h>
#include <rex/system/xthread.h>

REX_HOOK_RAW(__imp__NetDll_WSARecvFrom) {
  // The SDK has no receive implementation and returns SOCKET_ERROR without
  // setting last-error. Keep the failure explicit, including zero bytes read.
  if (ctx.r7.u32) *reinterpret_cast<rex::be<std::uint32_t>*>(base+ctx.r7.u32)=0;
  rex::system::XThread::SetLastError(10050); // WSAENETDOWN
  ctx.r3.u64=0xFFFFFFFFu;

}

REX_HOOK_RAW(__imp__NetDll_WSAGetOverlappedResult) {
  beatles::DeprioritizeOfflineNetworkThread();
  if (ctx.r6.u32) *reinterpret_cast<rex::be<std::uint32_t>*>(base + ctx.r6.u32) = 0;
  rex::system::XThread::SetLastError(10050);  // WSAENETDOWN.
  ctx.r3.u64 = 0;                          // FALSE, not a completed packet.
  static std::atomic<bool> reported{false};
  if (!reported.exchange(true)) REXLOG_INFO("Network overlapped request unavailable (offline)");
}
#include <windows.h>
#include <rex/ppc/stack.h>
REX_HOOK_RAW(__imp__NetDll_WSAWaitForMultipleEvents){
 const auto count=ctx.r3.u32,handles=ctx.r4.u32,wait_all=ctx.r5.u32,timeout_ms=ctx.r6.u32,alertable=ctx.r7.u32;
 if(!count||count>64||!handles){rex::system::XThread::SetLastError(87);ctx.r3.u64=0xFFFFFFFF;return;}
 static auto*wait=reinterpret_cast<PPCFunc*>(GetProcAddress(GetModuleHandleW(L"rexruntime.dll"),"__imp__NtWaitForMultipleObjectsEx"));
 if(!wait)REX_FATAL("Missing original NtWaitForMultipleObjectsEx");
 rex::ppc::stack_guard stack(ctx);
 std::uint32_t timeout_address=0;
 if(timeout_ms!=0xFFFFFFFF){
  const std::int64_t interval=-std::int64_t(timeout_ms)*10000;
  timeout_address=rex::ppc::stack_push(ctx,base,interval);
 }
 // WSA takes milliseconds and a BOOL; NtWait takes signed 100-ns ticks and
 // WaitAll=0 / WaitAny=1. Preserve timeout and signalled-event index results.
 do{
  ctx.r3.u64=count;ctx.r4.u64=handles;ctx.r5.u64=wait_all?0:1;
  ctx.r6.u64=1;ctx.r7.u64=alertable;ctx.r8.u64=timeout_address;
  wait(ctx,base);
 }while(ctx.r3.u32==0x101);
 if(ctx.r3.u32&0x80000000){rex::system::XThread::SetLastError(87);ctx.r3.u64=0xFFFFFFFF;}
 static std::atomic<bool> reported{false};
 if(!reported.exchange(true))REXLOG_INFO("Network event wait: corrected millisecond timeout and wait mode");
}


