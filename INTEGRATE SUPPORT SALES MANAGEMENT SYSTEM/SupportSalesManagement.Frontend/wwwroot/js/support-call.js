window.supportCall = (function () {
    const peers = new Map();
    const localStreams = new Map();
    const remoteAudioEls = new Map();
    const dotnetRefs = new Map();
    let configuredIceServers = [
        { urls: "stun:stun.l.google.com:19302" },
        { urls: "stun:stun1.l.google.com:19302" }
    ];

    async function ensurePeer(callId) {
        if (peers.has(callId)) return peers.get(callId);

        const peer = new RTCPeerConnection({ iceServers: configuredIceServers });
        peers.set(callId, peer);

        peer.onicecandidate = (event) => {
            if (!event.candidate) return;
            const ref = dotnetRefs.get(callId);
            if (ref) {
                ref.invokeMethodAsync("OnIceCandidateGenerated", callId, JSON.stringify(event.candidate)).catch(() => {});
            }
        };

        peer.ontrack = (event) => {
            const audioEl = remoteAudioEls.get(callId);
            if (!audioEl) return;
            audioEl.srcObject = event.streams[0];
            audioEl.play().catch(() => {});
        };

        return peer;
    }

    async function ensureLocalStream(callId) {
        if (localStreams.has(callId)) return localStreams.get(callId);
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true, video: false });
        localStreams.set(callId, stream);
        return stream;
    }

    async function initCall(callId, audioElementId, dotnetRef) {
        if (!navigator.mediaDevices || !window.RTCPeerConnection) {
            throw new Error("WebRTC is not supported in this browser.");
        }

        if (dotnetRef) {
            dotnetRefs.set(callId, dotnetRef);
        }

        const audioEl = document.getElementById(audioElementId);
        if (!audioEl) {
            throw new Error("Remote audio element not found.");
        }

        remoteAudioEls.set(callId, audioEl);

        const peer = await ensurePeer(callId);
        const stream = await ensureLocalStream(callId);

        stream.getTracks().forEach((track) => {
            if (!peer.getSenders().some((s) => s.track && s.track.id === track.id)) {
                peer.addTrack(track, stream);
            }
        });
    }

    async function createOffer(callId) {
        const peer = await ensurePeer(callId);
        const offer = await peer.createOffer();
        await peer.setLocalDescription(offer);
        return JSON.stringify(offer);
    }

    async function createAnswer(callId, offerJson) {
        const peer = await ensurePeer(callId);
        const offer = JSON.parse(offerJson);
        await peer.setRemoteDescription(new RTCSessionDescription(offer));
        const answer = await peer.createAnswer();
        await peer.setLocalDescription(answer);
        return JSON.stringify(answer);
    }

    async function setRemoteAnswer(callId, answerJson) {
        const peer = await ensurePeer(callId);
        const answer = JSON.parse(answerJson);
        await peer.setRemoteDescription(new RTCSessionDescription(answer));
    }

    async function addIceCandidate(callId, candidateJson) {
        const peer = await ensurePeer(callId);
        const candidate = JSON.parse(candidateJson);
        await peer.addIceCandidate(new RTCIceCandidate(candidate));
    }

    function endCall(callId) {
        const peer = peers.get(callId);
        if (peer) {
            peer.onicecandidate = null;
            peer.ontrack = null;
            peer.getSenders().forEach((s) => {
                try { s.track && s.track.stop(); } catch (_) {}
            });
            peer.close();
            peers.delete(callId);
        }

        const stream = localStreams.get(callId);
        if (stream) {
            stream.getTracks().forEach((t) => {
                try { t.stop(); } catch (_) {}
            });
            localStreams.delete(callId);
        }

        const el = remoteAudioEls.get(callId);
        if (el) {
            try {
                el.pause();
                el.srcObject = null;
            } catch (_) {}
            remoteAudioEls.delete(callId);
        }

        dotnetRefs.delete(callId);
    }

    function isSupported() {
        return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia && window.RTCPeerConnection);
    }

    function setIceServers(iceServers) {
        if (!Array.isArray(iceServers) || iceServers.length === 0) {
            return;
        }

        const normalized = iceServers
            .map((s) => {
                const urls = Array.isArray(s.urls) ? s.urls : [];
                if (urls.length === 0) return null;
                const item = { urls };
                if (s.username) item.username = s.username;
                if (s.credential) item.credential = s.credential;
                return item;
            })
            .filter(Boolean);

        if (normalized.length > 0) {
            configuredIceServers = normalized;
        }
    }

    return {
        initCall,
        createOffer,
        createAnswer,
        setRemoteAnswer,
        addIceCandidate,
        endCall,
        isSupported,
        setIceServers
    };
})();
