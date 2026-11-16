// Browser bridge for the WebGL build: the camera (getUserMedia), QR decoding in a Web Worker (jsQR, embedded by
// SpookyJsQR.jspre), prefers-reduced-motion, Screen Wake Lock and vibration.
//
// The camera is our own instead of Unity's WebCamTexture, which stays black in iOS Safari: Unity plays the stream in
// a <video> that is never added to the page and leaves its permission stream running. Here the video lives in the
// page (invisible, muted, inline), each new frame is drawn to a canvas, and that canvas feeds both the Unity texture
// and the QR worker.
mergeInto(LibraryManager.library, {
  $spookyCam: function () {
    if (!Module.spookyCam) {
      Module.spookyCam = {
        stream: null, video: null, canvas: null, context: null, decodeCanvas: null, decodeContext: null,
        // permission: 0 unknown, 1 granted, 2 denied. state: 0 closed, 1 starting, 2 playing, -1 failed.
        permission: 0, state: 0, token: 0, dirty: false, lastTime: -1, hasFrame: false
      };
    }
    return Module.spookyCam;
  },

  $spookyQrWorker: function () {
    var state = Module.spookyQr;
    if (!state) {
      state = Module.spookyQr = { worker: null, busy: false, result: null, failed: false };
    }
    if (state.worker || state.failed) return state;
    try {
      var handler =
        "self.onmessage = function (e) {" +
        "  var d = e.data, r = null;" +
        "  try { r = jsQR(new Uint8ClampedArray(d.buffer), d.w, d.h, { inversionAttempts: d.thorough ? 'attemptBoth' : 'dontInvert' }); } catch (x) {}" +
        "  self.postMessage({ text: r ? r.data : null });" +
        "};";
      var blob = new Blob([SPOOKY_JSQR_SOURCE, "\n", handler], { type: "application/javascript" });
      state.worker = new Worker(URL.createObjectURL(blob));
      state.worker.onmessage = function (e) {
        state.busy = false;
        if (e.data && e.data.text) state.result = e.data.text;
      };
      state.worker.onerror = function (e) {
        state.busy = false;
        console.warn("[SpookyQr] worker error", e && e.message);
      };
    } catch (e) {
      console.error("[SpookyQr] could not start the QR worker", e);
      state.failed = true;
    }
    return state;
  },

  SpookyCam_IsSupported: function () {
    return navigator.mediaDevices && navigator.mediaDevices.getUserMedia ? 1 : 0;
  },

  SpookyCam_Permission__deps: ["$spookyCam"],
  SpookyCam_Permission: function () {
    return spookyCam().permission;
  },

  // Opens the back camera (reusing the stream when it is still live). callback(code), if given:
  // 1 granted, 2 denied, 3 other failure (no camera, camera busy...).
  SpookyCam_Open__deps: ["$spookyCam"],
  SpookyCam_Open: function (callback) {
    var cam = spookyCam();
    var report = function (code) {
      if (callback) {{{ makeDynCall("vi", "callback") }}}(code);
    };

    var attach = function () {
      if (!cam.video) {
        var video = document.createElement("video");
        video.setAttribute("playsinline", "");
        video.setAttribute("webkit-playsinline", "");
        video.setAttribute("muted", "");
        video.setAttribute("autoplay", "");
        video.muted = true;
        video.playsInline = true;
        video.autoplay = true;
        // In the page but out of sight: iOS only delivers frames for videos that are part of the document.
        video.style.cssText = "position:fixed;left:0;top:0;width:2px;height:2px;opacity:0.01;pointer-events:none;z-index:-1;";
        document.body.appendChild(video);
        cam.video = video;
        // If autoplay was refused, the next touch starts the video.
        document.addEventListener("touchend", function () {
          if (cam.video && cam.stream && cam.video.paused) cam.video.play().catch(function () {});
        });
      }
      if (cam.video.srcObject !== cam.stream) cam.video.srcObject = cam.stream;
      cam.dirty = false;
      cam.lastTime = -1;
      cam.hasFrame = false;
      cam.state = 1;
      var play = cam.video.play();
      if (play && play.catch) play.catch(function (e) { console.warn("[SpookyCam] play() was refused", e); });

      if (cam.video.requestVideoFrameCallback && !cam.frameLoop) {
        cam.frameLoop = true;
        var onFrame = function () {
          cam.dirty = true;
          if (cam.video) cam.video.requestVideoFrameCallback(onFrame);
        };
        cam.video.requestVideoFrameCallback(onFrame);
      }
    };

    var live = cam.stream && cam.stream.getVideoTracks().some(function (track) { return track.readyState === "live"; });
    if (live) {
      attach();
      report(1);
      return;
    }

    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      cam.state = -1;
      report(3);
      return;
    }

    var token = ++cam.token;
    cam.state = 1;
    navigator.mediaDevices.getUserMedia({
      audio: false,
      video: { facingMode: { ideal: "environment" }, width: { ideal: 1280 }, height: { ideal: 720 } }
    }).then(function (stream) {
      if (token !== cam.token) {
        // Closed while the browser was asking: let the camera go again.
        stream.getTracks().forEach(function (track) { track.stop(); });
        return;
      }
      cam.stream = stream;
      cam.permission = 1;
      attach();
      report(1);
    }).catch(function (e) {
      if (token !== cam.token) return;
      var denied = e && (e.name === "NotAllowedError" || e.name === "SecurityError" || e.name === "PermissionDeniedError");
      if (denied) cam.permission = 2;
      cam.state = -1;
      console.warn("[SpookyCam] getUserMedia failed", e && e.name, e && e.message);
      report(denied ? 2 : 3);
    });
  },

  SpookyCam_Close__deps: ["$spookyCam"],
  SpookyCam_Close: function () {
    var cam = spookyCam();
    cam.token++;
    if (cam.stream) cam.stream.getTracks().forEach(function (track) { track.stop(); });
    cam.stream = null;
    if (cam.video) {
      cam.video.pause();
      cam.video.srcObject = null;
    }
    cam.state = 0;
    cam.hasFrame = false;
  },

  // 0 closed, 1 starting, 2 playing (frames have a size), -1 failed.
  SpookyCam_State__deps: ["$spookyCam"],
  SpookyCam_State: function () {
    var cam = spookyCam();
    if (cam.state === 1 && cam.video && cam.video.videoWidth > 0 && cam.video.videoHeight > 0) cam.state = 2;
    if (cam.state === 2 && cam.stream && !cam.stream.getVideoTracks().some(function (track) { return track.readyState === "live"; })) cam.state = -1;
    return cam.state;
  },

  // Frame size handed to Unity: the video size, capped to 1280 on the long side.
  SpookyCam_Width__deps: ["$spookyCam"],
  SpookyCam_Width: function () {
    var video = spookyCam().video;
    if (!video || !video.videoWidth) return 0;
    return Math.round(video.videoWidth * Math.min(1, 1280 / Math.max(video.videoWidth, video.videoHeight)));
  },

  SpookyCam_Height__deps: ["$spookyCam"],
  SpookyCam_Height: function () {
    var video = spookyCam().video;
    if (!video || !video.videoHeight) return 0;
    return Math.round(video.videoHeight * Math.min(1, 1280 / Math.max(video.videoWidth, video.videoHeight)));
  },

  // Copies the newest camera frame into the Unity texture (GL id from Texture.GetNativeTexturePtr) when the video has
  // a new one. Returns 1 when a new frame was uploaded.
  SpookyCam_Upload__deps: ["$spookyCam"],
  SpookyCam_Upload: function (textureId, width, height) {
    var cam = spookyCam();
    var video = cam.video;
    if (!video || cam.state !== 2 || video.readyState < 2) return 0;

    var fresh = cam.frameLoop ? cam.dirty : video.currentTime !== cam.lastTime;
    if (!fresh) return 0;
    cam.dirty = false;
    cam.lastTime = video.currentTime;

    if (!cam.canvas) {
      cam.canvas = document.createElement("canvas");
      cam.context = null;
    }
    if (cam.canvas.width !== width || cam.canvas.height !== height || !cam.context) {
      cam.canvas.width = width;
      cam.canvas.height = height;
      cam.context = cam.canvas.getContext("2d");
    }
    cam.context.drawImage(video, 0, 0, width, height);
    cam.hasFrame = true;

    var texture = GL.textures[textureId];
    if (!texture) return 0;
    var previous = GLctx.getParameter(GLctx.TEXTURE_BINDING_2D);
    GLctx.bindTexture(GLctx.TEXTURE_2D, texture);
    // Unity textures are bottom row first.
    GLctx.pixelStorei(GLctx.UNPACK_FLIP_Y_WEBGL, true);
    GLctx.texSubImage2D(GLctx.TEXTURE_2D, 0, 0, 0, GLctx.RGBA, GLctx.UNSIGNED_BYTE, cam.canvas);
    GLctx.pixelStorei(GLctx.UNPACK_FLIP_Y_WEBGL, false);
    GLctx.bindTexture(GLctx.TEXTURE_2D, previous);
    return 1;
  },

  // Sends the latest frame to the QR worker. The fast pass decodes the centre square (where the on-screen scan
  // window is) at up to 640 px; the thorough pass decodes the whole frame at up to 1024 px.
  SpookyCam_Decode__deps: ["$spookyCam", "$spookyQrWorker"],
  SpookyCam_Decode: function (thorough) {
    var cam = spookyCam();
    var qr = spookyQrWorker();
    if (qr.failed || qr.busy || !cam.hasFrame) return 0;

    var width = cam.canvas.width, height = cam.canvas.height;
    var sx = 0, sy = 0, sw = width, sh = height, maxSize = 1024;
    if (!thorough) {
      sw = sh = Math.round(Math.min(width, height) * 0.8);
      sx = Math.round((width - sw) / 2);
      sy = Math.round((height - sh) / 2);
      maxSize = 640;
    }
    var scale = Math.min(1, maxSize / Math.max(sw, sh));
    var dw = Math.max(1, Math.round(sw * scale)), dh = Math.max(1, Math.round(sh * scale));

    if (!cam.decodeCanvas) {
      cam.decodeCanvas = document.createElement("canvas");
      cam.decodeContext = null;
    }
    if (cam.decodeCanvas.width !== dw || cam.decodeCanvas.height !== dh || !cam.decodeContext) {
      cam.decodeCanvas.width = dw;
      cam.decodeCanvas.height = dh;
      cam.decodeContext = cam.decodeCanvas.getContext("2d", { willReadFrequently: true });
    }
    cam.decodeContext.drawImage(cam.canvas, sx, sy, sw, sh, 0, 0, dw, dh);
    var pixels = cam.decodeContext.getImageData(0, 0, dw, dh).data;

    qr.busy = true;
    qr.worker.postMessage({ buffer: pixels.buffer, w: dw, h: dh, thorough: thorough !== 0 }, [pixels.buffer]);
    return 1;
  },

  SpookyQr_IsBusy: function () {
    var state = Module.spookyQr;
    return state && state.busy ? 1 : 0;
  },

  SpookyQr_Poll: function () {
    var state = Module.spookyQr;
    if (!state || !state.result) return null;
    var text = state.result;
    state.result = null;
    var size = lengthBytesUTF8(text) + 1;
    var buffer = _malloc(size);
    stringToUTF8(text, buffer, size);
    return buffer;
  },

  SpookyWeb_PrefersReducedMotion: function () {
    try {
      return window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches ? 1 : 0;
    } catch (e) {
      return 0;
    }
  },

  SpookyWeb_KeepAwake: function () {
    try {
      if (!navigator.wakeLock) return;
      var request = function () {
        if (Module.spookyWakeLock || document.visibilityState !== "visible") return;
        navigator.wakeLock.request("screen").then(function (lock) {
          Module.spookyWakeLock = lock;
          lock.addEventListener("release", function () { Module.spookyWakeLock = null; });
        }).catch(function () {});
      };
      request();
      if (!Module.spookyWakeLockHooked) {
        Module.spookyWakeLockHooked = true;
        document.addEventListener("visibilitychange", request);
      }
    } catch (e) {}
  },

  SpookyWeb_Vibrate: function (milliseconds) {
    try {
      if (navigator.vibrate) navigator.vibrate(milliseconds);
    } catch (e) {}
  },

  // The page was opened by scanning a tag with the phone's own camera (…/?c=pack.id.SECRET): once the game caught it,
  // drop the code from the address bar so a reload or a Home Screen bookmark doesn't carry it around.
  SpookyWeb_ClearLaunchCode: function () {
    try {
      var url = new URL(window.location.href);
      if (!url.searchParams.has("c")) return;
      url.searchParams.delete("c");
      window.history.replaceState(null, document.title, url.pathname + (url.search || "") + url.hash);
    } catch (e) {}
  }
});
