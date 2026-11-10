// Compatibility for the title's one-block audio header operation at 0x82515E18.
// Its Xbox wrapper uses XeKeysSetKey and XeKeysAesCbc with a zero IV. These
// calls are stubs in this SDK. A single CBC block with a zero IV is ECB.
#include <windows.h>
#include <bcrypt.h>
#include <array>
#include <atomic>
#include <cstring>
#include <rex/hook.h>

namespace {

using Block = std::array<UCHAR, 16>;

// Format constants for MOGG methods 0 and 1, independently documented in
// Onyx's onyx-moggcrypt/cbits/keys.h (HvKeys). Green Day selects one with r6.
constexpr std::array<Block, 2> kAudioMethods{{
    {0x01, 0x22, 0x00, 0x38, 0xD2, 0x01, 0x78, 0x8B, 0xDD, 0xCD, 0xD0, 0xF0, 0xFE, 0x3E, 0x24, 0x7F},
    {0x51, 0x73, 0xAD, 0xE5, 0xB3, 0x99, 0xB8, 0x61, 0x58, 0x1A, 0xF9, 0xB8, 0x1E, 0xA7, 0xBE, 0xBF},
}};

bool DecodeAudioHeader(Block input, Block method, Block& output) {
  BCRYPT_ALG_HANDLE algorithm = nullptr;
  if (BCryptOpenAlgorithmProvider(&algorithm, BCRYPT_AES_ALGORITHM, nullptr, 0) < 0) {
    return false;
  }
  BCRYPT_KEY_HANDLE key = nullptr;
  wchar_t chaining_mode[] = BCRYPT_CHAIN_MODE_ECB;
  const bool key_created =
      BCryptSetProperty(algorithm, BCRYPT_CHAINING_MODE, reinterpret_cast<PUCHAR>(chaining_mode),
                        sizeof(chaining_mode), 0) >= 0 &&
      BCryptGenerateSymmetricKey(algorithm, &key, nullptr, 0, method.data(),
                                 static_cast<ULONG>(method.size()), 0) >= 0;
  ULONG written = 0;
  const bool decoded =
      key_created &&
      BCryptDecrypt(key, input.data(), static_cast<ULONG>(input.size()), nullptr, nullptr, 0,
                    output.data(), static_cast<ULONG>(output.size()), &written, 0) >= 0 &&
      written == output.size();
  if (key) BCryptDestroyKey(key);
  BCryptCloseAlgorithmProvider(algorithm, 0);
  return decoded;
}

}  // namespace

REX_HOOK_RAW(sub_82515E18) {
  const auto input_address = ctx.r4.u32;
  const auto output_address = ctx.r5.u32;
  if (!input_address || !output_address) {
    ctx.r3.u64 = ERROR_INVALID_PARAMETER;
    return;
  }

  const auto method = ctx.r6.u8 != 0;
  Block input{}, output{};
  std::memcpy(input.data(), base + input_address, input.size());
  if (!DecodeAudioHeader(input, kAudioMethods[method], output)) {
    REXLOG_ERROR("MOGG audio header: Windows AES could not decode the block");
    ctx.r3.u64 = ERROR_INVALID_DATA;
    return;
  }
  std::memcpy(base + output_address, output.data(), output.size());
  ctx.r3.u64 = ERROR_SUCCESS;

  static std::array<std::atomic<bool>, 2> reported{};
  if (!reported[method].exchange(true)) {
    REXLOG_INFO("MOGG audio header: Windows AES active for method {}", unsigned(method));
  }
}
